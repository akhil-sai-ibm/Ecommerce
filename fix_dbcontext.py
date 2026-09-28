import os

def replace_context(path):
    with open(path, "r", encoding="utf-8") as file:
        content = file.read()
    content = content.replace("ECommerceDbContext", "UserServiceDbContext")
    with open(path, "w", encoding="utf-8") as file:
        file.write(content)

repo_dir = "UserService/UserService.Infrastructure/Repositories"
for root, _, files in os.walk(repo_dir):
    for f in files:
        if f.endswith(".cs"):
            replace_context(os.path.join(root, f))

print("Replaced DbContext in Repositories.")
