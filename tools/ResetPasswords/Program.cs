using Microsoft.AspNetCore.Identity;

var hasher = new PasswordHasher<string>();
string hash = hasher.HashPassword("u", "MedFlow2026!");
System.Console.WriteLine(hash);
