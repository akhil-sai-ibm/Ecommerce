import os
import re

def replace_namespaces(content):
    content = content.replace("namespace ECommerce.", "namespace UserService.")
    content = content.replace("using ECommerce.", "using UserService.")
    # Fix fully qualified name
    content = content.replace("ECommerce.Application.DTOs.Common.FileData", "UserService.Application.DTOs.Common.FileData")
    return content

def migrate_file(src, dest):
    os.makedirs(os.path.dirname(dest), exist_ok=True)
    if os.path.exists(src):
        with open(src, 'r', encoding='utf-8') as f:
            content = f.read()
        content = replace_namespaces(content)
        with open(dest, 'w', encoding='utf-8') as f:
            f.write(content)
        print(f"Migrated {src} to {dest}")
    else:
        print(f"Warning: {src} not found!")

app_files = [
    "DTOs/Auth/RegisterRequest.cs",
    "DTOs/Auth/LoginRequest.cs",
    "DTOs/Auth/AuthResponse.cs",
    "DTOs/Users/UserProfileDto.cs",
    "DTOs/Users/UpdateUserProfileDto.cs",
    "DTOs/Common/FileData.cs"
]
for f in app_files:
    migrate_file(f"ECommerce.Application/{f}", f"UserService/UserService.Application/{f}")

print("Done migrating missing DTOs.")
