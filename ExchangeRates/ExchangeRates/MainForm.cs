using System;
using System.Collections.Generic;
using System.Data;
using System.IO;
using System.Net;
using System.Windows.Forms;
using System.Runtime.Serialization.Json;
using System.Data.SqlClient;

namespace ExchangeRates
{
    public partial class MainForm : Form
    {       
        SqlConnection connection;

        public MainForm()
        {
            InitializeComponent();
        }

        private void connectButton_Click(object sender, EventArgs e)
        {
            ConnectToDB();
            loadButton.Enabled = true;
            comboBox1.Enabled = true;
            connectButton.Enabled = false;
        }

        private void loadButton_Click(object sender, EventArgs e)
        {
            progressBar1.Value = 0;
            progressBar1.Maximum = (dateTimePicker2.Value - dateTimePicker1.Value).Days;

            OpenConnection(connection);
            for (DateTime dt = dateTimePicker1.Value; dt <= dateTimePicker2.Value; dt = dt.AddDays(1))
            {
                string date = dt.ToString("yyyy-M-d");
                List<Rate> RatesOnDate = GetRatesOnDate(date);
                foreach (Rate r in RatesOnDate)
                {
                    CreateTableIfNecessary(connection, r.Cur_Abbreviation);
                    InsertToTableIfNotExist(connection, r);
                }
                progressBar1.PerformStep();
            }
            GetTableNames(connection);
            comboBox1.SelectedIndex = 0;
            connection.Close();
        }

        private void comboBox1_SelectedValueChanged(object sender, EventArgs e)
        {
            if (connection.State == ConnectionState.Closed)
                OpenConnection(connection);
            ShowSelectedTable(comboBox1.SelectedItem.ToString());
            connection.Close();
        }

        private void ConnectToDB()
        {
            string connStr = @"Data Source=" + textBox1.Text + @";
                            Initial Catalog=Rates;
                            Integrated Security=True";
            connection = new SqlConnection(connStr);
            try
            {
                connection.Open();
                MessageBox.Show("Соедение успешно произведено", "Сообщение", MessageBoxButtons.OK, MessageBoxIcon.Information);
                connection.Close();
            }
            catch (SqlException se)
            {
                if (se.Number == 4060)  // Если база не обнаружена
                {
                    connection.Close();
                    using (connection = new SqlConnection(@"Data Source=" + textBox1.Text + @";Integrated Security=True"))
                    {
                        connection.Open();
                        SqlCommand cmdCreateDataBase = new SqlCommand("CREATE DATABASE [Rates]", connection);
                        cmdCreateDataBase.ExecuteNonQuery();
                        connection.Close();
                    }
                    connection = new SqlConnection(connStr);
                    MessageBox.Show("Соедение успешно произведено", "Сообщение", MessageBoxButtons.OK, MessageBoxIcon.Information);
                }
                else
                    MessageBox.Show(se.Message, "Ошибка", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }

        private void OpenConnection(SqlConnection connection)
        {
            try
            {
                connection.Open();
            }
            catch (SqlException se)
            {
                MessageBox.Show(se.Message, "Ошибка", MessageBoxButtons.OK, MessageBoxIcon.Error);
                return;
            }
        }

        private List<Rate> GetRatesOnDate(string date)
        {
            List<Rate> RatesOnDate = new List<Rate>();
            DataContractJsonSerializer jsonFormatter = new DataContractJsonSerializer(typeof(List<Rate>));
            WebRequest request = WebRequest.Create("http://www.nbrb.by/API/ExRates/Rates?onDate=" + date + "&Periodicity=0");
            request.ContentType = "text/json";
            request.Method = "GET";
            WebResponse response = request.GetResponse();
            using (Stream stream = response.GetResponseStream())
            {
                RatesOnDate = (List<Rate>)jsonFormatter.ReadObject(stream);
            }
            response.Close();
            return RatesOnDate;
        }

        private static void CreateTableIfNecessary(SqlConnection connection, string abr)
        {
            SqlCommand cmdCreateTable = new SqlCommand(
                @"IF OBJECT_ID('" + abr + @"','U') IS NULL
                     CREATE TABLE " + abr +
                        @" (Date		    datetime, 
                        Cur_ID			int NOT NULL, 
	                    Cur_Abbreviation varchar(100), 
	                    Cur_Scale		int, 
	                    Cur_Name		varchar(100), 
	                    Cur_OfficialRate decimal(18,4));", connection);
            try
            {
                cmdCreateTable.ExecuteNonQuery();
            }
            catch
            {
                MessageBox.Show("Ошибка при создании таблицы", "Ошибка", MessageBoxButtons.OK, MessageBoxIcon.Error);
                return;
            }
        }

        private static void InsertToTableIfNotExist(SqlConnection connection, Rate r)
        {
            SqlCommand cmd = new SqlCommand(
                "IF NOT EXISTS (SELECT 1 FROM " + r.Cur_Abbreviation + " WHERE Date = @date) " +
                    "INSERT INTO " + r.Cur_Abbreviation +
                        " VALUES(@date, @ID, '" + r.Cur_Abbreviation +
                        "', @scale, '" + r.Cur_Name + "', @rate)", connection);

            SqlParameter param = new SqlParameter("@date", SqlDbType.DateTime);
            param.Value = Convert.ToDateTime(r.Date);
            cmd.Parameters.Add(param);

            param = new SqlParameter("@ID", SqlDbType.Int);
            param.Value = r.Cur_ID;
            cmd.Parameters.Add(param);

            param = new SqlParameter("@scale", SqlDbType.Int);
            param.Value = r.Cur_Scale;
            cmd.Parameters.Add(param);

            param = new SqlParameter("@rate", SqlDbType.Decimal);
            param.Value = r.Cur_OfficialRate;
            cmd.Parameters.Add(param);

            try
            {
                cmd.ExecuteNonQuery();
            }
            catch
            {
                MessageBox.Show("Ошибка, при добавлении записи","Ошибка", MessageBoxButtons.OK, MessageBoxIcon.Error);
                return;
            }
        }

        private void GetTableNames(SqlConnection connection)
        {
            SqlCommand command = new SqlCommand("USE Rates; SELECT TABLE_NAME FROM information_schema.tables;", connection);
            using (SqlDataReader reader = command.ExecuteReader())
            {
                if (reader.HasRows)
                {
                    while (reader.Read())
                    {
                        comboBox1.Items.Add(reader.GetValue(0));
                    }
                }
            }
        }

        private void ShowSelectedTable(string abr)
        {
            string sql = "SELECT * FROM " + abr;
            SqlDataAdapter adapter = new SqlDataAdapter(sql, connection);
            DataSet ds = new DataSet();
            adapter.Fill(ds);
            dataGridView1.DataSource = ds.Tables[0];
        }    
    }
}
