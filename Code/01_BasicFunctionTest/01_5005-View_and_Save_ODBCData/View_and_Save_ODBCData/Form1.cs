﻿using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Windows.Forms;
using System.Threading;
using AdvWebUIAPI;
using System.Runtime.InteropServices;
using System.Diagnostics;
using ThirdPartyToolControl;
using iATester;
using CommonFunction;

namespace View_and_Save_ODBCData
{
    public partial class Form1 : Form, iATester.iCom
    {
        IAdvSeleniumAPI api;
        cThirdPartyToolControl tpc = new cThirdPartyToolControl();
        cEventLog EventLog = new cEventLog();

        private delegate void DataGridViewCtrlAddDataRow(DataGridViewRow i_Row);
        private DataGridViewCtrlAddDataRow m_DataGridViewCtrlAddDataRow;
        internal const int Max_Rows_Val = 65535;
        string baseUrl;
        string sIniFilePath = @"C:\WebAccessAutoTestSetting.ini";

        //Send Log data to iAtester
        public event EventHandler<LogEventArgs> eLog = delegate { };
        //Send test result to iAtester
        public event EventHandler<ResultEventArgs> eResult = delegate { };
        //Send execution status to iAtester
        public event EventHandler<StatusEventArgs> eStatus = delegate { };

        public void StartTest()
        {
            //Add test code
            long lErrorCode = 0;
            EventLog.AddLog("===View and Save ODBCData start (by iATester)===");
            if (System.IO.File.Exists(sIniFilePath))    // 再load一次
            {
                EventLog.AddLog(sIniFilePath + " file exist, load initial setting");
                InitialRequiredInfo(sIniFilePath);
            }
            EventLog.AddLog("Project= " + ProjectName.Text);
            EventLog.AddLog("WebAccess IP address= " + WebAccessIP.Text);
            lErrorCode = Form1_Load(ProjectName.Text, WebAccessIP.Text, TestLogFolder.Text, Browser.Text);
            EventLog.AddLog("===View and Save ODBCData end (by iATester)===");

            if (lErrorCode == 0)
                eResult(this, new ResultEventArgs(iResult.Pass));
            else
                eResult(this, new ResultEventArgs(iResult.Fail));

            eStatus(this, new StatusEventArgs(iStatus.Completion));
        }

        public Form1()
        {
            InitializeComponent();
            try
            {
                m_DataGridViewCtrlAddDataRow = new DataGridViewCtrlAddDataRow(DataGridViewCtrlAddNewRow);
            }
            catch (Exception ex)
            {
                MessageBox.Show(ex.ToString());
            }
            Browser.SelectedIndex = 0;
            if (System.IO.File.Exists(sIniFilePath))
            {
                EventLog.AddLog(sIniFilePath + " file exist, load initial setting");
                InitialRequiredInfo(sIniFilePath);
            }
        }

        long Form1_Load(string sProjectName, string sWebAccessIP, string sTestLogFolder, string sBrowser)
        {
            baseUrl = "http://" + sWebAccessIP;

            if (sBrowser == "Internet Explorer")
            {
                EventLog.AddLog("Browser= Internet Explorer");
                api = new AdvSeleniumAPI("IE", "");
                System.Threading.Thread.Sleep(1000);
            }
            else if (sBrowser == "Mozilla FireFox")
            {
                EventLog.AddLog("Browser= Mozilla FireFox");
                api = new AdvSeleniumAPI("FireFox", "");
                System.Threading.Thread.Sleep(1000);
            }

            api.LinkWebUI(baseUrl + "/broadWeb/bwconfig.asp?username=admin");
            api.ById("userField").Enter("").Submit().Exe();
            PrintStep("Login WebAccess");

            EventLog.AddLog("Check analog tag data...");
            bool bAnaChk = AnalogTagDataCheck(sProjectName);

            EventLog.AddLog("Check discrete tag data...");
            bool bDisChk = DiscreteTagDataCheck(sProjectName);

            /*
            EventLog.AddLog("Save data to excel");
            SaveDatatoExcel(sProjectName, sTestLogFolder);
            */
            api.Quit();
            PrintStep("Quit browser");

            bool bSeleniumResult = true;
            int iTotalSeleniumAction = dataGridView1.Rows.Count;
            for (int i = 0; i < iTotalSeleniumAction - 1; i++)
            {
                DataGridViewRow row = dataGridView1.Rows[i];
                string sSeleniumResult = row.Cells[2].Value.ToString();
                if (sSeleniumResult != "pass")
                {
                    bSeleniumResult = false;
                    EventLog.AddLog("Test Fail !!");
                    EventLog.AddLog("Fail TestItem = " + row.Cells[0].Value.ToString());
                    EventLog.AddLog("BrowserAction = " + row.Cells[1].Value.ToString());
                    EventLog.AddLog("Result = " + row.Cells[2].Value.ToString());
                    EventLog.AddLog("ErrorCode = " + row.Cells[3].Value.ToString());
                    EventLog.AddLog("ExeTime(ms) = " + row.Cells[4].Value.ToString());
                    break;
                }
            }

            if (bSeleniumResult && bAnaChk && bDisChk)
            {
                Result.Text = "PASS!!";
                Result.ForeColor = Color.Green;
                EventLog.AddLog("Test Result: PASS!!");
                return 0;
            }
            else
            {
                Result.Text = "FAIL!!";
                Result.ForeColor = Color.Red;
                EventLog.AddLog("Test Result: FAIL!!");
                return -1;
            }

            //return 0;
        }

        private bool AnalogTagDataCheck(string sProjectName)
        {
            bool bCheckAnalogTag = true;
            string[] ToBeTestTag = { "Acc_0007", "AT_AI0007", "AT_AO0007", "Calc_OPCDA", "ConAna_0007", "OPCDA_0007", "OPCUA_0007", "SystemSec_0007" };

            for (int i = 0; i < ToBeTestTag.Length; i++)
            {
                EventLog.AddLog("Go to Analog Tag log setting page");
                api.ByXpath("//a[contains(@href, '/broadWeb/syslog/LogPg.asp') and contains(@href, 'pos=analog')]").Click();

                // select project name
                EventLog.AddLog("select project name");
                api.ByName("ProjNameSel").SelectTxt(sProjectName).Exe();
                Thread.Sleep(3000);

                // set today as start date
                //string sToday = string.Format("{0:dd}", DateTime.Now);
                //int iToday = Int32.Parse(sToday);   // 為了讓讀出來的日期去掉第一個零 ex: "06" -> "6"
                //string ssToday = string.Format("{0}", iToday);
                string sToday = DateTime.Now.ToString("%d");
                api.ByName("DateStart").Click();
                Thread.Sleep(1000);
                api.ByTxt(sToday).Click();
                Thread.Sleep(1000);
                EventLog.AddLog("select start date to today: " + sToday);

                // select one tag to get ODBC data
                EventLog.AddLog("select " + ToBeTestTag[i] + " to get ODBC data");
                api.ById("alltags").Click();
                api.ById("TagNameSel").SelectTxt(ToBeTestTag[i]).Exe();
                api.ById("addtag").Click();
                api.ById("TagNameSelResult").SelectTxt(ToBeTestTag[i]).Exe();

                Thread.Sleep(1000);
                api.ByName("PageSizeSel").Enter("").Submit().Exe();
                PrintStep("Set and get analog ODBC tag data");
                EventLog.AddLog("Get " + ToBeTestTag[i] + " ODBC data");

                Thread.Sleep(10000); // wait to get ODBC data

                api.ByXpath("//*[@id=\"myTable\"]/thead[1]/tr/th[2]/a").Click();    // click time to sort data
                Thread.Sleep(10000);
                //api.ByXpath("//*[@id=\"myTable\"]/thead[1]/tr/th[3]/a").Click();    // click tagname to sort data
                //Thread.Sleep(5000);

                bool bRes = bCheckRecordTime(ToBeTestTag[i]);
                if (bRes == false)
                    bCheckAnalogTag = false;

                // print screen
                EventLog.PrintScreen(ToBeTestTag[i] + "_ODBCData");

                api.ByXpath("//*[@id=\"div1\"]/table/tbody/tr[1]/td[3]/a[5]/font").Click();     //return to homepage
            }

            return bCheckAnalogTag;
        }

        private bool bCheckRecordTime(string sTagName)
        {
            bool bChkTagName = true;
            bool bChkTime = true;

            string sRecordTagNameBefore = api.ByXpath("//*[@id=\"myTable\"]/tbody/tr[1]/td[3]").GetText();
            string sRecordTagName = api.ByXpath("//*[@id=\"myTable\"]/tbody/tr[2]/td[3]").GetText();
            string sRecordTagNameAfter = api.ByXpath("//*[@id=\"myTable\"]/tbody/tr[3]/td[3]").GetText();
            EventLog.AddLog(sTagName + " ODBC record TagName(Before): " + sRecordTagNameBefore);
            EventLog.AddLog(sTagName + " ODBC record TagName(Now): " + sRecordTagName);
            EventLog.AddLog(sTagName + " ODBC record TagName(After): " + sRecordTagNameAfter);
            if (sRecordTagNameBefore != sTagName && sRecordTagName != sTagName && sRecordTagNameAfter != sTagName)
            {
                bChkTagName = false;
                EventLog.AddLog(sTagName + " Record TagName check FAIL!!");
            }

            if (bChkTagName)
            {
                string sRecordTimeBefore = api.ByXpath("//*[@id=\"myTable\"]/tbody/tr[1]/td[2]").GetText();
                string sRecordTime = api.ByXpath("//*[@id=\"myTable\"]/tbody/tr[2]/td[2]").GetText();
                string sRecordTimeAfter = api.ByXpath("//*[@id=\"myTable\"]/tbody/tr[3]/td[2]").GetText();
                EventLog.AddLog(sTagName + " ODBC record time(Before): " + sRecordTimeBefore);
                EventLog.AddLog(sTagName + " ODBC record time(Now): " + sRecordTime);
                EventLog.AddLog(sTagName + " ODBC record time(After): " + sRecordTimeAfter);

                string[] sBefore_tmp = sRecordTimeBefore.Split(new string[] { ":" }, StringSplitOptions.RemoveEmptyEntries);
                string[] sNow_tmp = sRecordTime.Split(new string[] { ":" }, StringSplitOptions.RemoveEmptyEntries);
                string[] sAfter_tmp = sRecordTimeAfter.Split(new string[] { ":" }, StringSplitOptions.RemoveEmptyEntries);
                if (sRecordTimeBefore != "" && sRecordTime != "" && sRecordTimeAfter != "")
                {
                    if (Int32.Parse(sNow_tmp[1]) - Int32.Parse(sBefore_tmp[1]) == 2 &&      // 確認是否2分鐘寫ODBC一次
                        Int32.Parse(sAfter_tmp[1]) - Int32.Parse(sNow_tmp[1]) == 2)
                    {
                        EventLog.AddLog(sTagName + " Record time interval check PASS!!");
                    }
                    else if (Int32.Parse(sNow_tmp[1]) - Int32.Parse(sBefore_tmp[1]) == -58 &&      // 58-0-2
                        Int32.Parse(sAfter_tmp[1]) - Int32.Parse(sNow_tmp[1]) == 2)
                    {
                        EventLog.AddLog(sTagName + " Record time interval check PASS!!");
                    }
                    else if (Int32.Parse(sNow_tmp[1]) - Int32.Parse(sBefore_tmp[1]) == 2 &&      // 56-58-0
                        Int32.Parse(sAfter_tmp[1]) - Int32.Parse(sNow_tmp[1]) == -58)
                    {
                        EventLog.AddLog(sTagName + " Record time interval check PASS!!");
                    }
                    else
                    {
                        bChkTime = false;
                        EventLog.AddLog(sTagName + " Record time interval check FAIL!!");
                    }
                }
                else
                {
                    bChkTime = false;
                    EventLog.AddLog(sTagName + "Record time interval check FAIL!!");
                }
            }

            return bChkTagName && bChkTime;
        }   // for analog tag

        private bool DiscreteTagDataCheck(string sProjectName)
        {
            bool bCheckDiscreteTag = true;
            string[] ToBeTestTag = { "AT_DI0007", "AT_DO0007" };

            for (int i = 0; i < ToBeTestTag.Length; i++)
            {
                EventLog.AddLog("Go to Discrete Tag log setting page");
                api.ByXpath("//a[contains(@href, '/broadWeb/syslog/LogPg.asp') and contains(@href, 'pos=discrete')]").Click();

                // select project name
                EventLog.AddLog("select project name");
                api.ByName("ProjNameSel").SelectTxt(sProjectName).Exe();
                Thread.Sleep(3000);

                // set today as start date
                //string sToday = string.Format("{0:dd}", DateTime.Now);
                //int iToday = Int32.Parse(sToday);   // 為了讓讀出來的日期去掉第一個零 ex: "06" -> "6"
                //string ssToday = string.Format("{0}", iToday);
                string sToday = DateTime.Now.ToString("%d");
                api.ByName("DateStart").Click();
                Thread.Sleep(1000);
                api.ByTxt(sToday).Click();
                Thread.Sleep(1000);
                EventLog.AddLog("select start date to today: " + sToday);

                // set start/end time   // 由於離散點是資料有變化則會記錄一次 資料量很大 故設定時間為現在時間往前1分鐘
                string sTimeEnd = DateTime.Now.ToString("HH:mm:ss");
                string sTimeStart = DateTime.Now.AddMinutes(-1).ToString("HH:mm:ss");
                api.ByName("TimeStart").Clear();
                api.ByName("TimeStart").Enter(sTimeStart).Exe(); //HHmmss
                api.ByName("TimeEnd").Clear();
                api.ByName("TimeEnd").Enter(sTimeEnd).Exe();
                
                // select one tag to get ODBC data
                EventLog.AddLog("select " + ToBeTestTag[i] + " to get ODBC data");
                api.ById("alltags").Click();
                api.ById("TagNameSel").SelectTxt(ToBeTestTag[i]).Exe();
                api.ById("addtag").Click();
                api.ById("TagNameSelResult").SelectTxt(ToBeTestTag[i]).Exe();

                Thread.Sleep(1000);
                api.ByName("PageSizeSel").Enter("").Submit().Exe();
                PrintStep("Set and get discrete ODBC tag data");
                EventLog.AddLog("Get " + ToBeTestTag[i] + " ODBC data");

                Thread.Sleep(10000); // wait to get ODBC data

                bool bRes = bCheckRecordValue(ToBeTestTag[i]);
                if (bRes == false)
                    bCheckDiscreteTag = false;

                // print screen
                EventLog.PrintScreen(ToBeTestTag[i] + "_ODBCData");

                api.ByXpath("//*[@id=\"div1\"]/table/tbody/tr[1]/td[3]/a[5]/font").Click();     //return to homepage
            }
            return bCheckDiscreteTag;
        }

        private bool bCheckRecordValue(string sTagName)
        {
            bool bChkTagName = true;
            bool bChkValue = true;

            string sRecordTagNameBefore = api.ByXpath("//*[@id=\"myTable\"]/tbody/tr[1]/td[3]").GetText();
            string sRecordTagName = api.ByXpath("//*[@id=\"myTable\"]/tbody/tr[2]/td[3]").GetText();
            string sRecordTagNameAfter = api.ByXpath("//*[@id=\"myTable\"]/tbody/tr[3]/td[3]").GetText();
            EventLog.AddLog(sTagName + " ODBC record TagName(Before): " + sRecordTagNameBefore);
            EventLog.AddLog(sTagName + " ODBC record TagName(Now): " + sRecordTagName);
            EventLog.AddLog(sTagName + " ODBC record TagName(After): " + sRecordTagNameAfter);
            if (sRecordTagNameBefore != sTagName && sRecordTagName != sTagName && sRecordTagNameAfter != sTagName)
            {
                bChkTagName = false;
                EventLog.AddLog(sTagName + " Record TagName check FAIL!!");
            }

            if (bChkTagName)
            {
                string sRecordValueBefore = api.ByXpath("//*[@id=\"myTable\"]/tbody/tr[1]/td[5]").GetText();
                string sRecordValue = api.ByXpath("//*[@id=\"myTable\"]/tbody/tr[2]/td[5]").GetText();
                string sRecordValueAfter = api.ByXpath("//*[@id=\"myTable\"]/tbody/tr[3]/td[5]").GetText();
                EventLog.AddLog(sTagName + " ODBC record value(Before): " + sRecordValueBefore);
                EventLog.AddLog(sTagName + " ODBC record value(Now): " + sRecordValue);
                EventLog.AddLog(sTagName + " ODBC record value(After): " + sRecordValueAfter);

                if (sRecordValueBefore != "" && sRecordValue != "" && sRecordValueAfter != "")
                {
                    if (Math.Abs(Convert.ToDouble(sRecordValue) - Convert.ToDouble(sRecordValueBefore)) == 1 &&
                        Math.Abs(Convert.ToDouble(sRecordValueAfter) - Convert.ToDouble(sRecordValue)) == 1)
                    {
                        EventLog.AddLog(sTagName + " Record value interval check PASS!!");
                    }
                    else
                    {
                        bChkValue = false;
                        EventLog.AddLog(sTagName + " Record value interval check FAIL!!");
                    }
                }
                else
                {
                    bChkValue = false;
                    EventLog.AddLog(sTagName + "Record value interval check FAIL!!");
                }
            }

            return bChkTagName && bChkValue;
        }

        private void SaveDatatoExcel(string sProject, string sTestLogFolder)
        {
            string sUserName = Environment.UserName;
            string sourceFile = string.Format(@"C:\Users\{0}\Documents\ODBC_Temp.xlsx", sUserName);
            if (System.IO.File.Exists(sourceFile))
                System.IO.File.Delete(sourceFile);

            // Control browser
            int iIE_Handl = tpc.F_FindWindow("IEFrame", "WebAccess Analog Tag Log - Internet Explorer");
            int iIE_Handl_2 = tpc.F_FindWindowEx(iIE_Handl, 0, "Frame Tab", "");
            int iIE_Handl_3 = tpc.F_FindWindowEx(iIE_Handl_2, 0, "TabWindowClass", "WebAccess Analog Tag Log - Internet Explorer");
            int iIE_Handl_4 = tpc.F_FindWindowEx(iIE_Handl_3, 0, "Shell DocObject View", "");
            int iIE_Handl_5 = tpc.F_FindWindowEx(iIE_Handl_4, 0, "Internet Explorer_Server", "");

            if (iIE_Handl_5 > 0)
            {
                int x = 500;
                int y = 500;

                tpc.F_PostMessage(iIE_Handl_5, tpc.V_WM_RBUTTONDOWN, 0, (x & 0xFFFF) + (y & 0xFFFF) * 0x10000);
                //SendMessage(this.Handle, WM_LBUTTONDOWN, 0, (x & 0xFFFF) + (y & 0xFFFF) * 0x10000);
                Thread.Sleep(1000);
                tpc.F_PostMessage(iIE_Handl_5, tpc.V_WM_RBUTTONUP, 0, (x & 0xFFFF) + (y & 0xFFFF) * 0x10000);
                //SendMessage(this.Handle, WM_LBUTTONUP, 0, (x & 0xFFFF) + (y & 0xFFFF) * 0x10000);
                Thread.Sleep(1000);
                // save to excel
                SendKeys.SendWait("X"); // Export to excel
                Thread.Sleep(10000);
            }
            else
            {
                EventLog.AddLog("Cannot get Internet Explorer_Server page handle");
            }

            int iExcel = tpc.F_FindWindow("XLMAIN", "Microsoft Excel - 活頁簿1");
            if (iExcel > 0)                          // 讓開啟的Excel在最上層顯示
            {
                tpc.F_SetForegroundWindow(iExcel);
                Thread.Sleep(5000);
                SendKeys.SendWait("^s");    // save
                Thread.Sleep(2000);
                SendKeys.SendWait("ODBC_Temp");
                Thread.Sleep(500);
                SendKeys.SendWait("{ENTER}");
            }
            else
            {
                EventLog.AddLog("Could not find excel handle, excel may not be opened!");
            }

            EventLog.AddLog("Copy ODBC_Temp file to Test log folder ");
            string destFile = sTestLogFolder + string.Format("\\ODBCData_{0:yyyyMMdd_hhmmss}.xlsx", DateTime.Now);
            if (System.IO.File.Exists(sourceFile))
                System.IO.File.Copy(sourceFile, destFile, true);
            else
                EventLog.AddLog(string.Format("The file ( {0} ) is not found.", sourceFile));

            EventLog.AddLog("close excel start");
            Process[] processes = Process.GetProcessesByName("EXCEL");
            foreach (Process p in processes)
            {
                EventLog.AddLog("close excel...");
                p.WaitForExit(2000);
                //p.CloseMainWindow();
                p.Kill();
                p.Close();
            }
        }

        private void DataGridViewCtrlAddNewRow(DataGridViewRow i_Row)
        {
            if (this.dataGridView1.InvokeRequired)
            {
                this.dataGridView1.Invoke(new DataGridViewCtrlAddDataRow(DataGridViewCtrlAddNewRow), new object[] { i_Row });
                return;
            }

            this.dataGridView1.Rows.Insert(0, i_Row);
            if (dataGridView1.Rows.Count > Max_Rows_Val)
            {
                dataGridView1.Rows.RemoveAt((dataGridView1.Rows.Count - 1));
            }
            this.dataGridView1.Update();
        }

        private void ReturnSCADAPage()
        {
            api.SwitchToCurWindow(0);
            api.SwitchToFrame("leftFrame", 0);
            api.ByXpath("//a[contains(@href, '/broadWeb/bwMainRight.asp') and contains(@href, 'name=TestSCADA')]").Click();

        }

        private void PrintStep(string sTestItem)
        {
            DataGridViewRow dgvRow;
            DataGridViewCell dgvCell;

            var list = api.GetStepResult();
            foreach (var item in list)
            {
                AdvSeleniumAPI.ResultClass _res = (AdvSeleniumAPI.ResultClass)item;
                //
                dgvRow = new DataGridViewRow();
                if (_res.Res == "fail")
                    dgvRow.DefaultCellStyle.ForeColor = Color.Red;
                dgvCell = new DataGridViewTextBoxCell(); //Column Time
                //
                if (_res == null) continue;
                //
                dgvCell.Value = sTestItem;
                dgvRow.Cells.Add(dgvCell);
                //
                dgvCell = new DataGridViewTextBoxCell();
                dgvCell.Value = _res.Decp;
                dgvRow.Cells.Add(dgvCell);
                //
                dgvCell = new DataGridViewTextBoxCell();
                dgvCell.Value = _res.Res;
                dgvRow.Cells.Add(dgvCell);
                //
                dgvCell = new DataGridViewTextBoxCell();
                dgvCell.Value = _res.Err;
                dgvRow.Cells.Add(dgvCell);
                //
                dgvCell = new DataGridViewTextBoxCell();
                dgvCell.Value = _res.Tdev;
                dgvRow.Cells.Add(dgvCell);

                m_DataGridViewCtrlAddDataRow(dgvRow);
            }
            Application.DoEvents();
        }

        private void Start_Click(object sender, EventArgs e)
        {
            long lErrorCode = 0;
            EventLog.AddLog("===View and Save ODBCData start===");
            CheckifIniFileChange();
            EventLog.AddLog("Project= " + ProjectName.Text);
            EventLog.AddLog("WebAccess IP address= " + WebAccessIP.Text);
            lErrorCode = Form1_Load(ProjectName.Text, WebAccessIP.Text, TestLogFolder.Text, Browser.Text);
            EventLog.AddLog("===View and Save ODBCData end===");
        }

        private void InitialRequiredInfo(string sFilePath)
        {
            StringBuilder sDefaultProjectName1 = new StringBuilder(255);
            StringBuilder sDefaultProjectName2 = new StringBuilder(255);
            StringBuilder sDefaultIP1 = new StringBuilder(255);
            StringBuilder sDefaultIP2 = new StringBuilder(255);
            /*
            tpc.F_WritePrivateProfileString("ProjectName", "Ground PC or Primary PC", "TestProject", @"C:\WebAccessAutoTestSetting.ini");
            tpc.F_WritePrivateProfileString("ProjectName", "Cloud PC or Backup PC", "CTestProject", @"C:\WebAccessAutoTestSetting.ini");
            tpc.F_WritePrivateProfileString("IP", "Ground PC or Primary PC", "172.18.3.62", @"C:\WebAccessAutoTestSetting.ini");
            tpc.F_WritePrivateProfileString("IP", "Cloud PC or Backup PC", "172.18.3.65", @"C:\WebAccessAutoTestSetting.ini");
            */
            tpc.F_GetPrivateProfileString("ProjectName", "Ground PC or Primary PC", "NA", sDefaultProjectName1, 255, sFilePath);
            tpc.F_GetPrivateProfileString("ProjectName", "Cloud PC or Backup PC", "NA", sDefaultProjectName2, 255, sFilePath);
            tpc.F_GetPrivateProfileString("IP", "Ground PC or Primary PC", "NA", sDefaultIP1, 255, sFilePath);
            tpc.F_GetPrivateProfileString("IP", "Cloud PC or Backup PC", "NA", sDefaultIP2, 255, sFilePath);
            ProjectName.Text = sDefaultProjectName1.ToString();
            WebAccessIP.Text = sDefaultIP1.ToString();
        }

        private void CheckifIniFileChange()
        {
            StringBuilder sDefaultProjectName1 = new StringBuilder(255);
            StringBuilder sDefaultProjectName2 = new StringBuilder(255);
            StringBuilder sDefaultIP1 = new StringBuilder(255);
            StringBuilder sDefaultIP2 = new StringBuilder(255);
            if (System.IO.File.Exists(sIniFilePath))    // 比對ini檔與ui上的值是否相同
            {
                EventLog.AddLog(".ini file exist, check if .ini file need to update");
                tpc.F_GetPrivateProfileString("ProjectName", "Ground PC or Primary PC", "NA", sDefaultProjectName1, 255, sIniFilePath);
                tpc.F_GetPrivateProfileString("ProjectName", "Cloud PC or Backup PC", "NA", sDefaultProjectName2, 255, sIniFilePath);
                tpc.F_GetPrivateProfileString("IP", "Ground PC or Primary PC", "NA", sDefaultIP1, 255, sIniFilePath);
                tpc.F_GetPrivateProfileString("IP", "Cloud PC or Backup PC", "NA", sDefaultIP2, 255, sIniFilePath);

                if (ProjectName.Text != sDefaultProjectName1.ToString())
                {
                    tpc.F_WritePrivateProfileString("ProjectName", "Ground PC or Primary PC", ProjectName.Text, sIniFilePath);
                    EventLog.AddLog("New ProjectName update to .ini file!!");
                    EventLog.AddLog("Original ini:" + sDefaultProjectName1.ToString());
                    EventLog.AddLog("New ini:" + ProjectName.Text);
                }
                if (WebAccessIP.Text != sDefaultIP1.ToString())
                {
                    tpc.F_WritePrivateProfileString("IP", "Ground PC or Primary PC", WebAccessIP.Text, sIniFilePath);
                    EventLog.AddLog("New WebAccessIP update to .ini file!!");
                    EventLog.AddLog("Original ini:" + sDefaultIP1.ToString());
                    EventLog.AddLog("New ini:" + WebAccessIP.Text);
                }
            }
            else
            {
                EventLog.AddLog(".ini file not exist, create new .ini file. Path: " + sIniFilePath);
                tpc.F_WritePrivateProfileString("ProjectName", "Ground PC or Primary PC", ProjectName.Text, sIniFilePath);
                tpc.F_WritePrivateProfileString("ProjectName", "Cloud PC or Backup PC", "CTestProject", sIniFilePath);
                tpc.F_WritePrivateProfileString("IP", "Ground PC or Primary PC", WebAccessIP.Text, sIniFilePath);
                tpc.F_WritePrivateProfileString("IP", "Cloud PC or Backup PC", "172.18.3.65", sIniFilePath);
            }
        }
    }
}