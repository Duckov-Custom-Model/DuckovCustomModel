# -*- coding: utf-8 -*-
import json
import os
import sys
import time
import urllib.request
from datetime import datetime

# Fix encoding for Windows console
if sys.platform == 'win32':
    import io
    sys.stdout = io.TextIOWrapper(sys.stdout.buffer, encoding='utf-8', errors='replace')
    sys.stderr = io.TextIOWrapper(sys.stderr.buffer, encoding='utf-8', errors='replace')

def get_release_with_retry(api_url, headers, package_file_name="DuckovCustomModel.zip", max_retries=15, retry_delay=3):
    last_error = None
    release = None

    for attempt in range(1, max_retries + 1):
        try:
            print(f"尝试获取 Release 信息 ({attempt}/{max_retries})...")

            req = urllib.request.Request(api_url, headers=headers)
            with urllib.request.urlopen(req) as response:
                release = json.load(response)

            if not release.get('tag_name'):
                raise ValueError("Release 数据不完整：缺少 tag_name")

            assets = release.get('assets', [])
            package_asset = next((a for a in assets if a.get('name') == package_file_name), None)

            if package_asset:
                print("✓ Release 信息获取成功，assets 已上传")
                print(f"  找到打包文件: {package_asset.get('name')}")
                print(f"  下载链接: {package_asset.get('browser_download_url')}")
                return release

            if attempt < max_retries:
                print(f"⚠ Assets 尚未上传完成，等待 {retry_delay} 秒后重试...")
                time.sleep(retry_delay)
            else:
                print("⚠ 已达到最大重试次数，assets 可能尚未上传完成")
                return release

        except urllib.error.HTTPError as e:
            last_error = e
            status_code = e.code

            print(f"⚠ 尝试 {attempt}/{max_retries} 失败")
            print(f"  HTTP 状态码: {status_code}")
            print(f"  错误信息: {str(e)}")

            if status_code == 404 and attempt < max_retries:
                print(f"等待 {retry_delay} 秒后重试...")
                time.sleep(retry_delay)
            elif status_code == 403:
                print("API 访问被拒绝，可能是限流或权限问题")
                if attempt < max_retries:
                    time.sleep(retry_delay)
            elif attempt >= max_retries:
                raise Exception(f"获取 Release 信息失败，已重试 {max_retries} 次: {str(e)}")
        except Exception as e:
            last_error = e
            print(f"⚠ 尝试 {attempt}/{max_retries} 失败: {str(e)}")
            if attempt >= max_retries:
                raise Exception(f"获取 Release 信息失败，已重试 {max_retries} 次: {str(e)}")
            time.sleep(retry_delay)

    if not release:
        raise Exception(f"获取 Release 信息失败，已重试 {max_retries} 次: {last_error}")

    return release

def format_datetime_to_iso8601(dt_value):
    if isinstance(dt_value, str):
        try:
            for fmt in ["%Y-%m-%dT%H:%M:%SZ", "%Y-%m-%dT%H:%M:%S.%fZ", "%Y-%m-%dT%H:%M:%S%z"]:
                try:
                    dt = datetime.strptime(dt_value.replace('Z', '+00:00'), fmt.replace('Z', '%z'))
                    return dt.strftime("%Y-%m-%dT%H:%M:%S.000Z")
                except:
                    continue
            dt = datetime.fromisoformat(dt_value.replace('Z', '+00:00'))
            return dt.strftime("%Y-%m-%dT%H:%M:%S.000Z")
        except Exception as e:
            print(f"⚠ 无法解析日期格式: {dt_value}，使用原始值")
            return dt_value
    else:
        return dt_value

def parse_download_links(assets, package_file_name="DuckovCustomModel.zip"):
    download_links = []

    if assets:
        package_asset = next((a for a in assets if a.get('name') == package_file_name), None)
        if package_asset and package_asset.get('browser_download_url'):
            download_links.append({
                'name': 'Github Release',
                'url': package_asset.get('browser_download_url')
            })

    return download_links

github_token = os.environ.get('GITHUB_TOKEN')
github_repository = os.environ.get('GITHUB_REPOSITORY')
tag_name = os.environ.get('TAG_NAME')

if not github_token or not github_repository or not tag_name:
    print("::error::缺少必要的环境变量", file=sys.stderr)
    sys.exit(1)

api_url = f"https://api.github.com/repos/{github_repository}/releases/tags/{tag_name}"

print("获取 Release 信息...")
print(f"Tag: {tag_name}")
print(f"API URL: {api_url}")

headers = {
    'Authorization': f'token {github_token}',
    'Accept': 'application/vnd.github.v3+json'
}

release = get_release_with_retry(api_url, headers, max_retries=15, retry_delay=3)

version = release.get('tag_name', '').lstrip('v')
release_name = release.get('name') or f"Release {version}"
published_at = format_datetime_to_iso8601(release.get('published_at', ''))
changelog = release.get('body') or ''
download_links = parse_download_links(release.get('assets', []))

print("Release 信息:")
print(f"  版本: {version}")
print(f"  名称: {release_name}")
print(f"  发布时间: {published_at}")
print(f"  更新日志: {'已设置 (' + str(len(changelog)) + ' 字符)' if changelog else '未设置'}")
print(f"  下载链接: {'已找到 (' + str(len(download_links)) + ' 项)' if download_links else '未找到'}")

output_file = os.environ.get('GITHUB_OUTPUT')
if output_file:
    with open(output_file, 'a', encoding='utf-8') as f:
        f.write(f"version={version}\n")
        f.write(f"release_name={release_name}\n")
        f.write(f"published_at={published_at}\n")
        delimiter_changelog = f"CHANGELOG_EOF_{os.urandom(8).hex()}"
        f.write(f"changelog<<{delimiter_changelog}\n")
        f.write(changelog)
        f.write(f"\n{delimiter_changelog}\n")
        delimiter_download = f"DOWNLOAD_LINKS_EOF_{os.urandom(8).hex()}"
        f.write(f"download_links<<{delimiter_download}\n")
        f.write(json.dumps(download_links, ensure_ascii=False))
        f.write(f"\n{delimiter_download}\n")
else:
    print("::error::GITHUB_OUTPUT 环境变量未设置", file=sys.stderr)
    sys.exit(1)
