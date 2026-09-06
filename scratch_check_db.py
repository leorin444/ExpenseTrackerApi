import pyodbc

conn_str = "DRIVER={ODBC Driver 17 for SQL Server};SERVER=192.168.0.106;DATABASE=expense_tracker;UID=sa;PWD=Le0rin44;TrustServerCertificate=Yes;"
try:
    conn = pyodbc.connect(conn_str)
    cursor = conn.cursor()
    cursor.execute("SELECT Id, Email, PasswordHash FROM Users WHERE Email='monkey@gmai.com'")
    row = cursor.fetchone()
    if row:
        print(f"Id: {row.Id}")
        print(f"Email: {row.Email}")
        print(f"PasswordHash: {row.PasswordHash}")
    else:
        print("User not found.")
except Exception as e:
    print(f"Error: {e}")
