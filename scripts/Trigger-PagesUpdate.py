# -*- coding: utf-8 -*-
import json
import os
import sys
import time
import urllib.request

# Fix encoding for Windows console
if sys.platform == 'win32':
    import io
    sys.stdout = io.TextIOWrapper(sys.stdout.buffer, encoding='utf-8', errors='replace')
    sys.stderr = io.TextIOWrapper(sys.stderr.buffer, encoding='utf-8', errors='replace')

def parse_download_links_from_json(json_str):
    if not json_str or not json_str.strip():
        return None

    trimmed = json_str.strip()
    if trimmed in ['[]', 'null', 'undefined', '']:
        return None

    try:
        parsed = json.loads(trimmed)

        if isinstance(parsed, list):
            valid_links = []
            for item in parsed:
                if isinstance(item, dict) and 'name' in item and 'url' in item:
                    name = str(item.get('name', ''))
                    url = str(item.get('url', ''))
                    if name and url:
                        valid_links.append({
                            'name': name,
                            'url': url
                        })
            return valid_links if valid_links else None
        elif isinstance(parsed, dict):
            if 'name' in parsed and 'url' in parsed:
                name = str(parsed.get('name', ''))
                url = str(parsed.get('url', ''))
                if name and url:
                    return [{'name': name, 'url': url}]

        return None
    except Exception as e:
        print(f"⚠ download_links JSON 解析失败: {e}", file=sys.stderr)
        return None

def invoke_repository_dispatch(api_url, headers, payload, max_retries=15, retry_delay=5):
    body = {
        'event_type': 'update-release',
        'client_payload': payload
    }
    body_json = json.dumps(body, ensure_ascii=False)
    body_bytes = body_json.encode('utf-8')

    last_error = None

    for attempt in range(1, max_retries + 1):
        try:
            print(f"尝试触发页面更新 ({attempt}/{max_retries})...")

            req = urllib.request.Request(api_url, data=body_bytes, headers=headers, method='POST')
            with urllib.request.urlopen(req) as response:
                status_code = response.getcode()
                # GitHub API returns 204 No Content on success for repository_dispatch
                if status_code == 204:
                    print("✓ 页面更新触发成功 (204 No Content)")
                    return True

                # Try to read response body if available
                try:
                    response_body = response.read().decode('utf-8')
                    if response_body:
                        response_data = json.loads(response_body)
                        print("✓ 页面更新触发成功")
                        print(f"响应: {json.dumps(response_data, ensure_ascii=False)}")
                    else:
                        print(f"✓ 页面更新触发成功 (状态码: {status_code}, 无响应体)")
                    return True
                except json.JSONDecodeError:
                    # Response is not JSON, but status code indicates success
                    print(f"✓ 页面更新触发成功 (状态码: {status_code}, 非 JSON 响应)")
                    return True
        except urllib.error.HTTPError as e:
            last_error = e
            status_code = e.code

            print(f"⚠ 尝试 {attempt}/{max_retries} 失败", file=sys.stderr)
            print(f"  HTTP 状态码: {status_code}", file=sys.stderr)
            print(f"  错误信息: {str(e)}", file=sys.stderr)

            if e.fp:
                try:
                    response_body = e.fp.read().decode('utf-8')
                    print(f"  响应内容: {response_body}", file=sys.stderr)
                except:
                    print("  无法读取响应内容", file=sys.stderr)

            if attempt < max_retries:
                print(f"等待 {retry_delay} 秒后重试...")
                time.sleep(retry_delay)
        except Exception as e:
            last_error = e
            print(f"⚠ 尝试 {attempt}/{max_retries} 失败: {str(e)}", file=sys.stderr)
            if attempt < max_retries:
                print(f"等待 {retry_delay} 秒后重试...")
                time.sleep(retry_delay)

    raise Exception(f"页面更新触发失败，已重试 {max_retries} 次: {last_error}")

pages_repo_owner = os.environ.get('PAGES_REPO_OWNER')
pages_repo_name = os.environ.get('PAGES_REPO_NAME')
pages_repo_token = os.environ.get('GITHUB_TOKEN')

required_secrets = {
    'PAGES_REPO_OWNER': pages_repo_owner,
    'PAGES_REPO_NAME': pages_repo_name,
    'PAGES_REPO_TOKEN': pages_repo_token
}

for key, value in required_secrets.items():
    if not value or not value.strip():
        print(f"::error::{key} secret is not set", file=sys.stderr)
        sys.exit(1)

client_payload = {
    'version': os.environ.get('RELEASE_VERSION', ''),
    'release_name': os.environ.get('RELEASE_NAME', ''),
    'published_at': os.environ.get('RELEASE_PUBLISHED_AT', '')
}

changelog = os.environ.get('RELEASE_CHANGELOG', '')
if changelog:
    client_payload['changelog'] = changelog

download_links = parse_download_links_from_json(os.environ.get('RELEASE_DOWNLOAD_LINKS', ''))
if download_links:
    client_payload['download_links'] = download_links

headers = {
    'Authorization': f'token {pages_repo_token}',
    'Accept': 'application/vnd.github.v3+json',
    'Content-Type': 'application/json'
}

api_url = f"https://api.github.com/repos/{pages_repo_owner}/{pages_repo_name}/dispatches"

print("准备触发页面更新...")
print(f"目标仓库: {pages_repo_owner}/{pages_repo_name}")
print(f"版本: {client_payload.get('version')}")
print(f"更新日志: {'已包含 (' + str(len(changelog)) + ' 字符)' if changelog else '未包含'}")
print(f"下载链接: {'已包含 (' + str(len(download_links)) + ' 项)' if download_links else '未包含'}")
print(f"API URL: {api_url}")

invoke_repository_dispatch(api_url, headers, client_payload, max_retries=15, retry_delay=5)
