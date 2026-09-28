import os
import re
import shutil

def replace_namespaces(content):
    content = content.replace("namespace ECommerce.", "namespace UserService.")
    content = content.replace("using ECommerce.", "using UserService.")
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

# Domain
domain_files = [
    "Entities/User.cs",
    "Entities/Role.cs",
    "Entities/Address.cs",
    "Entities/BaseEntity.cs",
    "Enums/UserRole.cs",
    "Exceptions/DomainException.cs"
]
for f in domain_files:
    migrate_file(f"ECommerce.Domain/{f}", f"UserService/UserService.Domain/{f}")

# Application
app_files = [
    "DTOs/Users/RegisterUserDto.cs",
    "DTOs/Users/UpdateUserDto.cs",
    "DTOs/Users/UserDto.cs",
    "Interfaces/Repositories/IGenericRepository.cs",
    "Interfaces/Repositories/IUserRepository.cs",
    "Interfaces/Repositories/IUnitOfWork.cs",
    "Interfaces/Services/IAuthService.cs",
    "Interfaces/Services/ICurrentUserService.cs",
    "Interfaces/Services/IUserService.cs",
    "Interfaces/Services/IPasswordHasher.cs",
    "Interfaces/Services/ITokenService.cs",
    "Services/AuthService.cs",
    "Services/UserService.cs"
]
for f in app_files:
    migrate_file(f"ECommerce.Application/{f}", f"UserService/UserService.Application/{f}")

# Infrastructure
infra_files = [
    "Authentication/AspNetCorePasswordHasher.cs",
    "Authentication/JwtSettings.cs",
    "Authentication/JwtTokenService.cs",
    "Data/Configurations/AddressConfiguration.cs",
    "Data/Configurations/BaseEntityConfiguration.cs",
    "Data/Configurations/RoleConfiguration.cs",
    "Data/Configurations/UserConfiguration.cs",
    "Repositories/GenericRepository.cs",
    "Repositories/UserRepository.cs",
    "Repositories/UnitOfWork.cs",
    "Services/LocalFileStorageService.cs"
]
for f in infra_files:
    migrate_file(f"ECommerce.Infrastructure/{f}", f"UserService/UserService.Infrastructure/{f}")

# API
api_files = [
    "Controllers/AuthController.cs",
    "Controllers/UsersController.cs"
]
for f in api_files:
    migrate_file(f"ECommerce.API/{f}", f"UserService/UserService.API/{f}")

# Also need IFileStorageService which is in ECommerce.Application/Interfaces/Services/IFileStorageService.cs (Wait, let's check if it exists there or not. I'll search for it later)
# Wait, LocalFileStorageService implements IFileStorageService. Let me check if it's there.
try:
    if os.path.exists("ECommerce.Application/Interfaces/Services/IFileStorageService.cs"):
        migrate_file("ECommerce.Application/Interfaces/Services/IFileStorageService.cs", "UserService/UserService.Application/Interfaces/Services/IFileStorageService.cs")
except:
    pass

print("Done migration script.")
