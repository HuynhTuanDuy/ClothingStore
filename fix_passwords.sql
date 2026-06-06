SET QUOTED_IDENTIFIER ON; UPDATE ACCOUNTS SET PasswordHash = '$2a$11$8k3OA2is3aYf0NFRGROEjOt3uPyP3GZo9t3Fzb4ayDkN97NmOXZuW' WHERE UserName IN ('admin', 'manager', 'staff', 'user_vna', 'user_ttb');
