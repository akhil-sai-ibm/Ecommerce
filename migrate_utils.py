import os

def replace_namespaces(content):
    content = content.replace("namespace ECommerce.", "namespace UserService.")
    content = content.replace("using ECommerce.", "using UserService.")
    return content

utils_dir = "UserService/UserService.Application/Utils"
for root, _, files in os.walk(utils_dir):
    for f in files:
        if f.endswith(".cs"):
            path = os.path.join(root, f)
            with open(path, "r", encoding="utf-8") as file:
                content = file.read()
            content = replace_namespaces(content)
            with open(path, "w", encoding="utf-8") as file:
                file.write(content)
            print(f"Migrated {path}")
