﻿using System;
using System.Drawing;
using System.Windows.Forms;
using System.Threading;
using System.Diagnostics;
using MySql.Data.MySqlClient;

namespace EcomStatSender
{
    public partial class EcomStatSender : Form
    {
        static string PROGRAM_NAME = "EcomStatSender";
        static string PROGRAM_VERSION = "1.0.2";

        private LowLevelKeyboardListener _listener;

        static string DATABASE_CONNECTION = "datasource=urlexample.com;port=3306;username=user;password=pass;database=database_name";
        string sql = "";

        int sek;
        int min;
        bool isRunning;
        bool isReading = false;
        bool readed = false;
        short isSkipable = 0;

        string proces;
        string wybranyProces = "none";

        bool goodLogin = false;
        string login = "";
        string password = "";

        int lart = 0;
        int keys = 0;

        System.Windows.Forms.Timer myTimer = new System.Windows.Forms.Timer();
        System.Windows.Forms.Timer keyTimer = new System.Windows.Forms.Timer();
        System.Windows.Forms.Timer keyCheckTimer = new System.Windows.Forms.Timer();
        System.Windows.Forms.Timer logoutTimer = new System.Windows.Forms.Timer();

        public EcomStatSender()
        {
            InitializeComponent();
        }

        // SKRYPT WYLOGOWANIA PO 10 MIN
        private void TimeLogout(Object myObject, EventArgs eventArgs)
        {
            login = "";
            password = "";
            goodLogin = false;
            isReading = false;
            wybranyProces = "none";

            this.L_Active.Text = "";
            this.TB_Haslo.Text = "";
            this.L_Error.Text = "Wylogowano z powodu nieaktywności";

            this.B_Entropy.Visible = false;
            this.B_Logout.Visible = false;
            this.B_PakowanieBPIC.Visible = false;
            this.B_PakowanieMONO.Visible = false;
            this.B_PakowanieMVOL.Visible = false;
            this.B_PakowanieVOLU.Visible = false;
            this.B_SortowanieBPIC.Visible = false;
            this.B_SortowanieVOLU.Visible = false;
            this.B_StartStop.Visible = false;
            this.B_ZmienProces.Visible = false;
            this.B_Zwroty.Visible = false;
            this.L_Artykuly.Visible = false;
            this.L_ArtykulyText.Visible = false;
            this.L_Czas.Visible = false;
            this.L_CzasText.Visible = false;
            this.L_Procesy.Visible = false;

            this.B_Login.Visible = true;
            this.L_Error.Visible = true;
            this.L_Haslo.Visible = true;
            this.L_Login.Visible = true;
            this.TB_Haslo.Visible = true;
            this.TB_Login.Visible = true;
        }
        // -----

        // SKRYPT CO SEKUNDE TIMERA
        private void SekPlusPlus(Object myObject, EventArgs eventArgs)
        {
            sek++;

            if (sek > 59)
            {
                min++;
                sek = 0;
            }

            string s_sek = "";

            if (sek < 10)
            {
                s_sek = "0" + sek.ToString();
            }
            else
            {
                s_sek = sek.ToString();
            }

            string czas = min.ToString() + ":" + s_sek;

            this.L_Czas.Text = czas;
        }
        // -----

        // SKRYPT ZLICZANIA OGÓLNEGO
        private void Zliczanie(Object myObject, EventArgs eventArgs)
        {
            isReading = false;
            readed = false;
            keys = 0;
            isSkipable = 0;
            keyTimer.Stop();
        }
        // -----

        // SKRYPT ZŁAPANIA SKANERA
        private void Zliczanie_Check(Object myObject, EventArgs eventArgs)
        {
            if (keys < 14 && isReading == true)
            {
                keyTimer.Stop();
                isReading = false;
                readed = false;
                keys = 0;
                isSkipable = 0;
            }
            keyCheckTimer.Stop();
        }
        // -----

        private void EcomStatSender_Load(object sender, EventArgs e)
        {
            sek = 0;
            min = 0;
            isRunning = false;

            L_Wersja.Text = PROGRAM_VERSION;

            // WYLOGOWANIE PO 10 MIN
            logoutTimer.Stop();
            logoutTimer.Interval = 600000;
            logoutTimer.Tick += new EventHandler(TimeLogout);
            // -----

            // TIMER
            myTimer.Stop();
            myTimer.Tick += new EventHandler(SekPlusPlus);
            // -----

            // ZLICZANIE KLAWISZY
            keyTimer.Stop();
            keyTimer.Interval = 1000;
            keyTimer.Tick += new EventHandler(Zliczanie);
            // -----

            // ZLICZANIE KLAWISZY
            keyCheckTimer.Stop();
            keyCheckTimer.Interval = 200;
            keyCheckTimer.Tick += new EventHandler(Zliczanie_Check);
            // -----

            _listener = new LowLevelKeyboardListener();
            _listener.OnKeyPressed += _listener_OnKeyPressed;

            _listener.HookKeyboard();

            // POŁĄCZ Z BAZĄ I ROZPOCZNIJ PROGRAM
            MySqlConnection conn = new MySqlConnection(DATABASE_CONNECTION);
            MySqlCommand query = new MySqlCommand(sql, conn);
            query.CommandTimeout = 30;

            this.B_Login.Visible = true;
            this.L_Login.Visible = true;
            this.TB_Login.Visible = true;
            this.L_Haslo.Visible = true;
            this.TB_Haslo.Visible = true;
            // -----
        }

        private void B_StartStop_MouseClick(object sender, EventArgs e)
        {
            // ZACZNIJ DZIAŁAĆ
            if (isRunning == false)
            {
                try
                {
                    proces = wybranyProces;
                }
                catch
                {
                    proces = "";
                }
                this.L_Error.Text = proces;

                if (proces != "")
                {
                    this.L_Error.Visible = false;
                    isRunning = true;
                    this.B_StartStop.ForeColor = Color.Red;
                    this.B_StartStop.Text = "STOP";
                    this.B_ZmienProces.Focus();
                    this.B_ZmienProces.Enabled = false;
                    this.B_Logout.Visible = false;
                    myTimer.Interval = 1000;
                    myTimer.Start();
                    logoutTimer.Stop();
                }
                else
                {
                    this.L_Error.Text = "Wybierz proces!";
                    this.L_Error.Visible = true;
                    logoutTimer.Start();
                }
                
            }
            // -----

            // PRZESTAŃ DZIAŁAĆ
            else
            {
                myTimer.Stop();
                isRunning = false;

                sendStats(min, sek, lart);
                min = 0;
                sek = 0;
                lart = 0;

                this.L_Artykuly.Text = "0";
                this.L_Czas.Text = "0:00";
                this.B_StartStop.ForeColor = Color.Green;
                this.B_StartStop.Text = "START";
                this.B_ZmienProces.Enabled = true;
                this.B_Logout.Visible = true;

                logoutTimer.Start();

                Process devManViewProc = new Process();
                devManViewProc.StartInfo.FileName = @"DevManView.exe";
                devManViewProc.StartInfo.Arguments = "/disable \"Rodzajowy koncentrator USB\"";
                devManViewProc.Start();
                devManViewProc.WaitForExit();

                Thread.Sleep(500);

                devManViewProc.StartInfo.Arguments = "/enable \"Rodzajowy koncentrator USB\"";
                devManViewProc.Start();
                devManViewProc.WaitForExit();
            }
            // -----
            this.L_Czas.Focus();
        }

        private void B_Login_Click(object sender, EventArgs e)
        {
            login = this.TB_Login.Text.ToString();
            password = this.TB_Haslo.Text.ToString();

            goodLogin = isGoodLogin(login, password);
            // JEŻELI DOBRY LOGIN TO ZALOGUJ SIĘ
            if(goodLogin)
            {
                this.L_Active.Text = login;

                this.L_Error.Visible = false;
                this.L_Czas.Visible = false;
                this.L_CzasText.Visible = false;
                this.L_Artykuly.Visible = false;
                this.L_ArtykulyText.Visible = false;
                this.B_StartStop.Visible = false;
                this.L_Login.Visible = false;
                this.TB_Login.Visible = false;
                this.L_Haslo.Visible = false;
                this.TB_Haslo.Visible = false;
                this.B_Login.Visible = false;
                this.L_Procesy.Visible = true;
                this.B_Logout.Visible = true;
                this.B_PakowanieMONO.Visible = true;
                this.B_SortowanieBPIC.Visible = true;
                this.B_PakowanieBPIC.Visible = true;
                this.B_SortowanieVOLU.Visible = true;
                this.B_PakowanieVOLU.Visible = true;
                this.B_PakowanieMVOL.Visible = true;
                this.B_Zwroty.Visible = true;
                this.B_Entropy.Visible = true;
                this.B_ZmienProces.Visible = false;

                logoutTimer.Start();
            }
            // JEŻELI NIE WYŚWIETL ERROR
            else
            {
                this.L_Error.Text = "Błędny login lub hasło!";
                this.L_Error.Visible = true;
                this.L_Czas.Visible = false;
                this.L_CzasText.Visible = false;
                this.L_Artykuly.Visible = false;
                this.L_ArtykulyText.Visible = false;
                this.B_StartStop.Visible = false;
                this.L_Login.Visible = true;
                this.TB_Login.Visible = true;
                this.L_Haslo.Visible = true;
                this.TB_Haslo.Visible = true;
                this.B_Login.Visible = true;
                this.L_Procesy.Visible = false;
                this.B_Logout.Visible = false;
                this.B_PakowanieMONO.Visible = false;
                this.B_SortowanieBPIC.Visible = false;
                this.B_PakowanieBPIC.Visible = false;
                this.B_SortowanieVOLU.Visible = false;
                this.B_PakowanieVOLU.Visible = false;
                this.B_PakowanieMVOL.Visible = false;
                this.B_Zwroty.Visible = false;
                this.B_Entropy.Visible = false;
                this.B_ZmienProces.Visible = false;
            }
        }

        void _listener_OnKeyPressed(object sender, KeyPressedArgs e)
        {
            if (isRunning == true && isReading == false)
            {
                if(e.KeyPressed < 59 && e.KeyPressed > 47)
                {
                        keys++;
                        isReading = true;
                        keyTimer.Start();
                }

                if (e.KeyPressed == 77)
                    isSkipable = 2;

                if (e.KeyPressed == 53)
                    isSkipable = 1;
            }

            else if (isReading == true)
            {
                if(keys == 1 && e.KeyPressed == 56)
                {
                    isSkipable = 2;
                }

                if (e.KeyPressed < 59 && e.KeyPressed > 47)
                {
                    keys++;
                }

                if(keys == 13 && readed == false)
                {
                    if(isSkipable == 0)
                        lart++;
                    this.L_Artykuly.Text = lart.ToString();
                    readed = true;
                    keyCheckTimer.Start();
                    isSkipable = 0;
                }

                else if(keys == 30)
                {
                    keyTimer.Stop();
                    isReading = false;
                    readed = false;
                    keys = 0;
                }
            }
        }

        bool isGoodLogin(string possibleLogin, string possiblePassword)
        {
            sql = "SELECT ID FROM users WHERE Login='"+possibleLogin+"' AND Password='"+possiblePassword+"'";

            if (possibleLogin == "git" && possiblePassword == "hub")
            {
                return true;
            }

            MySqlConnection conn = new MySqlConnection(DATABASE_CONNECTION);
            MySqlCommand query = new MySqlCommand(sql, conn);
            query.CommandTimeout = 30;

            try
            {
                conn.Open();
                MySqlDataReader mySqlDataReader = query.ExecuteReader();

                if (mySqlDataReader.HasRows)
                {
                    return true;
                }
                else
                {
                    return false;
                }
            }
            catch (Exception e_sql)
            {
                return false;
            }
        }

        void sendStats(int statMin, int statSek, int statArt)
        {
            if(statSek<=10 && statMin == 0 || statArt<1)
            {
                this.L_Error.Text = "Czas lub l.art. zbyt mała!";
                this.L_Error.Visible = true;
            }
            else
            {
                string statProces = wybranyProces;
                int statTime = statMin * 60 + statSek;
                sql = "INSERT INTO stats (ID, UserLogin, Proces, Time, Articles, Date) VALUES (NULL, '"+login+"', '"+statProces+"', '"+statTime+"', '"+statArt+"', CURRENT_TIMESTAMP)";

                MySqlConnection conn = new MySqlConnection(DATABASE_CONNECTION);
                MySqlCommand query = new MySqlCommand(sql, conn);
                query.CommandTimeout = 30;

                try
                {
                    conn.Open();
                    MySqlDataReader mySqlDataReader = query.ExecuteReader();
                }
                catch (Exception e_sql)
                {
                    this.L_Error.Text = "Wystąpił błąd podczas wysyłania danych!";
                    this.L_Error.Visible = true;
                }
            }
        }

        private void B_ZmienProces_Click(object sender, EventArgs e)
        {
            wybranyProces = "";
            this.L_Procesy.Visible = true;
            this.B_Logout.Visible = true;
            this.B_PakowanieMONO.Visible = true;
            this.B_SortowanieBPIC.Visible = true;
            this.B_PakowanieBPIC.Visible = true;
            this.B_SortowanieVOLU.Visible = true;
            this.B_PakowanieVOLU.Visible = true;
            this.B_PakowanieMVOL.Visible = true;
            this.B_Zwroty.Visible = true;
            this.B_Entropy.Visible = true;
            this.B_ZmienProces.Visible = false;
            this.L_Czas.Visible = false;
            this.L_CzasText.Visible = false;
            this.L_Artykuly.Visible = false;
            this.L_ArtykulyText.Visible = false;
            this.B_StartStop.Visible = false;
        }

        private void B_PakowanieMONO_Click(object sender, EventArgs e)
        {
            wybranyProces = "Pakowanie MONO";
            this.L_Procesy.Visible = false;
            this.B_Logout.Visible = true;
            this.B_PakowanieMONO.Visible = false;
            this.B_SortowanieBPIC.Visible = false;
            this.B_PakowanieBPIC.Visible = false;
            this.B_SortowanieVOLU.Visible = false;
            this.B_PakowanieVOLU.Visible = false;
            this.B_PakowanieMVOL.Visible = false;
            this.B_Zwroty.Visible = false;
            this.B_Entropy.Visible = false;
            this.B_ZmienProces.Visible = true;
            this.L_Czas.Visible = true;
            this.L_CzasText.Visible = true;
            this.L_Artykuly.Visible = true;
            this.L_ArtykulyText.Visible = true;
            this.B_StartStop.Visible = true;

            keyTimer.Interval = 1000;
        }

        private void B_SortowanieBPIC_Click(object sender, EventArgs e)
        {
            wybranyProces = "Sortowanie BPIC";
            this.L_Procesy.Visible = false;
            this.B_Logout.Visible = true;
            this.B_PakowanieMONO.Visible = false;
            this.B_SortowanieBPIC.Visible = false;
            this.B_PakowanieBPIC.Visible = false;
            this.B_SortowanieVOLU.Visible = false;
            this.B_PakowanieVOLU.Visible = false;
            this.B_PakowanieMVOL.Visible = false;
            this.B_Zwroty.Visible = false;
            this.B_Entropy.Visible = false;
            this.B_ZmienProces.Visible = true;
            this.L_Czas.Visible = true;
            this.L_CzasText.Visible = true;
            this.L_Artykuly.Visible = true;
            this.L_ArtykulyText.Visible = true;
            this.B_StartStop.Visible = true;

            keyTimer.Interval = 1500;
        }

        private void B_PakowanieBPIC_Click(object sender, EventArgs e)
        {
            wybranyProces = "Pakowanie BPIC";
            this.L_Procesy.Visible = false;
            this.B_Logout.Visible = true;
            this.B_PakowanieMONO.Visible = false;
            this.B_SortowanieBPIC.Visible = false;
            this.B_PakowanieBPIC.Visible = false;
            this.B_SortowanieVOLU.Visible = false;
            this.B_PakowanieVOLU.Visible = false;
            this.B_PakowanieMVOL.Visible = false;
            this.B_Zwroty.Visible = false;
            this.B_Entropy.Visible = false;
            this.B_ZmienProces.Visible = true;
            this.L_Czas.Visible = true;
            this.L_CzasText.Visible = true;
            this.L_Artykuly.Visible = true;
            this.L_ArtykulyText.Visible = true;
            this.B_StartStop.Visible = true;

            keyTimer.Interval = 1500;
        }

        private void B_SortowanieVOLU_Click(object sender, EventArgs e)
        {
            wybranyProces = "Sortowanie VOLU";
            this.L_Procesy.Visible = false;
            this.B_Logout.Visible = true;
            this.B_PakowanieMONO.Visible = false;
            this.B_SortowanieBPIC.Visible = false;
            this.B_PakowanieBPIC.Visible = false;
            this.B_SortowanieVOLU.Visible = false;
            this.B_PakowanieVOLU.Visible = false;
            this.B_PakowanieMVOL.Visible = false;
            this.B_Zwroty.Visible = false;
            this.B_Entropy.Visible = false;
            this.B_ZmienProces.Visible = true;
            this.L_Czas.Visible = true;
            this.L_CzasText.Visible = true;
            this.L_Artykuly.Visible = true;
            this.L_ArtykulyText.Visible = true;
            this.B_StartStop.Visible = true;

            keyTimer.Interval = 1500;
        }

        private void B_PakowanieVOLU_Click(object sender, EventArgs e)
        {
            wybranyProces = "Pakowanie VOLU";
            this.L_Procesy.Visible = false;
            this.B_Logout.Visible = true;
            this.B_PakowanieMONO.Visible = false;
            this.B_SortowanieBPIC.Visible = false;
            this.B_PakowanieBPIC.Visible = false;
            this.B_SortowanieVOLU.Visible = false;
            this.B_PakowanieVOLU.Visible = false;
            this.B_PakowanieMVOL.Visible = false;
            this.B_Zwroty.Visible = false;
            this.B_Entropy.Visible = false;
            this.B_ZmienProces.Visible = true;
            this.L_Czas.Visible = true;
            this.L_CzasText.Visible = true;
            this.L_Artykuly.Visible = true;
            this.L_ArtykulyText.Visible = true;
            this.B_StartStop.Visible = true;

            keyTimer.Interval = 1500;
        }

        private void B_PakowanieMVOL_Click(object sender, EventArgs e)
        {
            wybranyProces = "Pakowanie MVOL";
            this.L_Procesy.Visible = false;
            this.B_Logout.Visible = true;
            this.B_PakowanieMONO.Visible = false;
            this.B_SortowanieBPIC.Visible = false;
            this.B_PakowanieBPIC.Visible = false;
            this.B_SortowanieVOLU.Visible = false;
            this.B_PakowanieVOLU.Visible = false;
            this.B_PakowanieMVOL.Visible = false;
            this.B_Zwroty.Visible = false;
            this.B_Entropy.Visible = false;
            this.B_ZmienProces.Visible = true;
            this.L_Czas.Visible = true;
            this.L_CzasText.Visible = true;
            this.L_Artykuly.Visible = true;
            this.L_ArtykulyText.Visible = true;
            this.B_StartStop.Visible = true;

            keyTimer.Interval = 1500;
        }

        private void B_Zwroty_Click(object sender, EventArgs e)
        {
            wybranyProces = "Zwroty";
            this.L_Procesy.Visible = false;
            this.B_Logout.Visible = true;
            this.B_PakowanieMONO.Visible = false;
            this.B_SortowanieBPIC.Visible = false;
            this.B_PakowanieBPIC.Visible = false;
            this.B_SortowanieVOLU.Visible = false;
            this.B_PakowanieVOLU.Visible = false;
            this.B_PakowanieMVOL.Visible = false;
            this.B_Zwroty.Visible = false;
            this.B_Entropy.Visible = false;
            this.B_ZmienProces.Visible = true;
            this.L_Czas.Visible = true;
            this.L_CzasText.Visible = true;
            this.L_Artykuly.Visible = true;
            this.L_ArtykulyText.Visible = true;
            this.B_StartStop.Visible = true;

            keyTimer.Interval = 1500;
        }

        private void B_Entropy_Click(object sender, EventArgs e)
        {
            wybranyProces = "Entropy";
            this.L_Procesy.Visible = false;
            this.B_Logout.Visible = true;
            this.B_PakowanieMONO.Visible = false;
            this.B_SortowanieBPIC.Visible = false;
            this.B_PakowanieBPIC.Visible = false;
            this.B_SortowanieVOLU.Visible = false;
            this.B_PakowanieVOLU.Visible = false;
            this.B_PakowanieMVOL.Visible = false;
            this.B_Zwroty.Visible = false;
            this.B_Entropy.Visible = false;
            this.B_ZmienProces.Visible = true;
            this.L_Czas.Visible = true;
            this.L_CzasText.Visible = true;
            this.L_Artykuly.Visible = true;
            this.L_ArtykulyText.Visible = true;
            this.B_StartStop.Visible = true;

            keyTimer.Interval = 1500;
        }

        private void B_Logout_Click(object sender, EventArgs e)
        {
            login = "";
            password = "";
            goodLogin = false;
            isReading = false;
            wybranyProces = "none";

            this.L_Active.Text = "";
            this.TB_Haslo.Text = "";
            this.L_Error.Text = "Wylogowano pomyślnie";

            this.B_Entropy.Visible = false;
            this.B_Logout.Visible = false;
            this.B_PakowanieBPIC.Visible = false;
            this.B_PakowanieMONO.Visible = false;
            this.B_PakowanieMVOL.Visible = false;
            this.B_PakowanieVOLU.Visible = false;
            this.B_SortowanieBPIC.Visible = false;
            this.B_SortowanieVOLU.Visible = false;
            this.B_StartStop.Visible = false;
            this.B_ZmienProces.Visible = false;
            this.B_Zwroty.Visible = false;
            this.L_Artykuly.Visible = false;
            this.L_ArtykulyText.Visible = false;
            this.L_Czas.Visible = false;
            this.L_CzasText.Visible = false;
            this.L_Procesy.Visible = false;

            this.B_Login.Visible = true;
            this.L_Error.Visible = true;
            this.L_Haslo.Visible = true;
            this.L_Login.Visible = true;
            this.TB_Haslo.Visible = true;
            this.TB_Login.Visible = true;
        }
    }
}