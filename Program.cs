// See https://aka.ms/new-console-template for more information
using ADO.NET_Demo;
using System.Data.SqlClient;

static string promptPassword()
{
    string password1, password2;
    while(true)
    {
        Console.Write("New password: ");
        password1 = Console.ReadLine() ?? "";
        Console.Write("New password (repeat to confirm): ");
        password2 = Console.ReadLine() ?? "";
        if (password1 == password2)
            return password1;
        Console.WriteLine("Passwords do not match, please try again");
    }
}

static bool confirmAction(string action)
{
    Console.Write(action);
    Console.Write(" [y/n]: ");
    bool yes = Console.ReadKey(true).Key == ConsoleKey.Y;
    Console.WriteLine();
    return yes;
}

UserPassAuth auth = new();
if(!auth.TableExists())
{
    Console.WriteLine("Table does not exist, creating it now...");
    auth.CreateTables();
    Console.WriteLine("Creating default user 'admin' with password 'admin'...");
    auth.CreateUser("admin", "admin");
}

string username;

logout:
while (true)
{
    Console.WriteLine("==== Login ====");
    Console.Write("Username: ");
    username = Console.ReadLine() ?? "";
    Console.Write("Password: ");
    string password = Console.ReadLine() ?? "";
    if (auth.TryLoginUser(username, password))
        break;
    Console.WriteLine("Login failed, please try again");
}

while (true)
{
    Console.WriteLine("Please select an option from the menu below");
    Console.WriteLine(" 1 = Create new user");
    Console.WriteLine(" 2 = Change password");
    Console.WriteLine(" 3 = Delete user");
    Console.WriteLine(" 7 = Delete user database (reset)");
    Console.WriteLine(" 8 = Log out");
    Console.WriteLine(" 9 = Exit");
    string npass;
    if (Int32.TryParse(Console.ReadLine() ?? "", out int choice))
        switch (choice)
        {
            case 1:
                Console.Write("New username: ");
                string nuser = Console.ReadLine() ?? "";
                npass = promptPassword();
                if (auth.CreateUser(nuser, npass))
                    Console.WriteLine("User created successfully");
                else
                    Console.WriteLine("Failed to create user (That username might already be taken)");
                break;
            case 2:
                npass = promptPassword();
                if (auth.ChangeUserPassword(username, npass))
                    Console.WriteLine("Password changed successfully");
                else
                    Console.WriteLine("Failed to change password");
                break;
            case 3:
                if(confirmAction("Are you sure you would like to delete this user?"))
                {
                    if(auth.DeleteUser(username))
                    {
                        Console.WriteLine("User deleted successfully");
                        goto case 8;
                    }
                    else
                    {
                        Console.WriteLine("Failed to delete user");
                        break;
                    }
                }
                else
                {
                    Console.WriteLine("Cancelled");
                    break;
                }
            case 7:
                if(confirmAction("Are you sure you would like to delete user database (this will delete all users)?"))
                {
                    auth.DropTables();
                    Console.WriteLine("Database deleted, ending program");
                    goto end;
                }
                else
                {
                    Console.WriteLine("Cancelled");
                    break;
                }
                
            case 8:
                goto logout;
            case 9:
                goto end;
    }
    else
        Console.WriteLine("Invalid entry, please try again");
}


end:

auth.CloseConnection();
