
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Data.SQLite;
using System.Data;

namespace SchaxxDiscordBot
{
    class SQLInstance
    {
        public static SQLiteConnection conn = new SQLiteConnection("Data Source=database.db; Version=3;New=True;Compress=True;");
        public static void open()
        {
            conn.Open();
            SQLiteCommand cmd = new SQLiteCommand(@"
            CREATE TABLE IF NOT EXISTS users (token CHAR, value INT);
            ", conn);
            cmd.ExecuteNonQuery();
        }
        public static void insertUser(String token, int value)
        {
            SQLiteCommand cmd = new SQLiteCommand(@"
            INSERT INTO table (token, value)
            VALUES( @token, @value );
            ", conn);
            cmd.Parameters.Add("@token", DbType.String);
            cmd.Parameters["@token"].Value = token;

            cmd.Parameters.Add("@value", DbType.Int32);
            cmd.Parameters["@value"].Value = value;

            cmd.ExecuteNonQuery();
        }
    }
}
