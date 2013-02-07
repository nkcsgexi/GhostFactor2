using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using MySql.Data.MySqlClient;
using NLog;
using warnings.util;

namespace SourceCommentAnalyzer
{
    class MySqlOperation
    {
        private readonly MySqlConnection connection;
        private readonly MySqlCommand command;
        private readonly Logger logger;

        public MySqlOperation()
        {
            string connString = "Server=127.0.0.1;Port=3306;Database=comments;Uid=root;password=939902;";
            this.connection = new MySqlConnection(connString);
            this.connection.Open();
            this.command = this.connection.CreateCommand();
            this.logger = NLoggerUtil.GetNLogger(typeof (MySqlOperation));
        }

        public void Close()
        {
            connection.Close();
        }

        public void CreateTable()
        {
            command.CommandText = @"CREATE TABLE Comments
                                (
                                    ProjectName varchar(30),
                                    CommitTime DATETIME,
                                    AuthorEmail varchar(60),
                                    CommitterEmail varchar(60),
                                    FileName varchar(50),
                                    CommentText varchar(500)
                                )";
            command.Prepare();
            command.ExecuteNonQuery();
        }

        public void DropTable()
        {
            command.CommandText = @"DROP TABLE Comments;";
            command.Prepare();
            command.ExecuteNonQuery();
        }

        public void InsertRecord(string project, DateTimeOffset time, string author, string committer, string 
            fileName, string comment)
        {
            // To shorten the comment if it is longer than the cell.
            comment = comment.Length > 499 ? comment.Substring(0, 499) : comment;

            // Replace any occurrence of '.
            comment = comment.Replace('\'', '"');
            command.CommandText = @"INSERT INTO Comments VALUES(" +
                "'" + project + "'" + "," +
                "'" + Format2SqlTime(time) + "'" + "," +
                "'" + author + "'" + "," +
                "'" + committer + "'" + "," +
                "'" + fileName + "'" + "," +
                "'" + comment + "'" + ");";
            logger.Info(command.CommandText);
            command.Prepare();
            command.ExecuteNonQuery();
        }


        private string Format2SqlTime(DateTimeOffset time)
        {
            // Sql DATETIME format is YYYY-MM-DD HH:MM:SS
            var date = time.Year + "-" + AddZeroBeforeSingleDigit(time.Month) + "-" + 
                AddZeroBeforeSingleDigit(time.Day);
            var stamp = AddZeroBeforeSingleDigit(time.Hour) + ":" + AddZeroBeforeSingleDigit(time.Minute) +
                ":" + AddZeroBeforeSingleDigit(time.Second);
            return date + " " + stamp;
        }

        private string AddZeroBeforeSingleDigit(int d)
        {
            return d < 10 ? "0" + d : d.ToString();
        }

    }
}
