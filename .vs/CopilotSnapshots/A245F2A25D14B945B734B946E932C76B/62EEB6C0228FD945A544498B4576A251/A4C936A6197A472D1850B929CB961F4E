using BCrypt.Net;

// See https://aka.ms/new-console-template for more information
Console.WriteLine("Hello, World!");

var existingHash = "$2a$11$wBHBpKnudJZ9U1yNZT5l5u4xGkVmI0QRjvEm0ZQ76C8/3Ha7jKOaC";

Console.WriteLine($"Verify admin vs existing: {BCrypt.Net.BCrypt.Verify("admin", existingHash)}");
Console.WriteLine($"Verify password vs existing: {BCrypt.Net.BCrypt.Verify("password", existingHash)}");
Console.WriteLine($"Verify admin123 vs existing: {BCrypt.Net.BCrypt.Verify("admin123", existingHash)}");

Console.WriteLine("Hash for 'admin' (work factor 11):");
Console.WriteLine(BCrypt.Net.BCrypt.HashPassword("admin", workFactor: 11));
