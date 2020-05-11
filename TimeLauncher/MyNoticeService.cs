using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using Android.App;
using Android.Content;
using Android.OS;
using Android.Runtime;
using Android.Views;
using Android.Widget;
using Xamarin.Essentials;
using Android.Util;
using Java.Lang;


namespace TimeLauncher
{
    [Service]
    class MyNoticeService : Service
    {
        static public string EXTRA_NAME = "extra_to_MyNoticeService";
        static public bool evningDakokuDone;

        private DateTime nextNotice;
        private DateTime appStartTime;

        private string _smtpServer;
        private string _smtpPass;
        private string _smtpAccount;
        private string _smtpMailToRaw;
        private int _smtpPort;

        private bool morningNoticeDone;

        public override IBinder OnBind(Intent intent)
        {
            return null;
        }

        [return: GeneratedEnum]
        public override StartCommandResult OnStartCommand(Intent intent, [GeneratedEnum] StartCommandFlags flags, int startId)
        {
            Log.Debug("MyNoticeService", "service started!!");

            var get_value = intent.GetStringArrayExtra(EXTRA_NAME);
            appStartTime = DateTime.Parse(get_value[0]);
            _smtpMailToRaw = get_value[1];
            _smtpServer = get_value[2];
            _smtpPort = int.Parse(get_value[3]);
            _smtpAccount = get_value[4];
            _smtpPass = get_value[5];

            nextNotice = appStartTime.Add(new TimeSpan(8, 15, 0));    //本番用　 
            // nextNotice = appStartTime.Add(new TimeSpan(0, 1, 0));       //テスト用 1分後に通知を出す

            Log.Debug("MyNoticeService", "next time:" + nextNotice.ToShortTimeString());

            morningNoticeDone = false;

            var t = new System.Threading.Thread(() =>
            {
                while (true)
                {
                    //朝の打刻時間バイブ
                    if (!morningNoticeDone && 
                        appStartTime.TimeOfDay < DateTime.Now.TimeOfDay && 
                        DateTime.Today.DayOfWeek != DayOfWeek.Saturday && 
                        DateTime.Today.DayOfWeek != DayOfWeek.Sunday)
                    {
                        //通知実施
                        try
                        {
                            Vibration.Vibrate(1000);
                            Log.Debug("MyNoticeService", "朝のバイブした");
                        }
                        catch (FeatureNotSupportedException)
                        {
                            Log.Debug("MyNoticeService", "バイブ失敗");
                        }

                        //一度通知したらその日は朝の通知を行わないよう、通知実施済みフラグを立てる
                        morningNoticeDone = true;
                    }

                    //日付が変わったら通知実施済みフラグを戻す
                    if (DateTime.Now.TimeOfDay < appStartTime.TimeOfDay)
                    {
                        morningNoticeDone = false;
                        evningDakokuDone = false;
                    }

                    //打刻忘れ通知
                    if (nextNotice.TimeOfDay < DateTime.Now.TimeOfDay && !evningDakokuDone)
                    {
                        //トースト表示 java.langなスレッドが必要みたい
                        new LooperThread("打刻忘れ？？").Start();
                        Log.Debug("MyNoticeService", "toast表示した");
                        Console.WriteLine("toast表示した");

                        //メール送信してみる
                        SendMail();
                        Log.Debug("MyNoticeService", "打刻忘れ通知のメール送信済み");

                        try
                        {
                            Vibration.Vibrate(2000);
                            Log.Debug("MyNoticeService", "打刻忘れ通知のバイブした");
                        }
                        catch (FeatureNotSupportedException)
                        {
                            // バイブしなくても致命的な問題ではないので、例外処理は何もしない
                            Log.Debug("MyNoticeService", "バイブ失敗");
                        }

                        // 次の通知時刻を設定
                        if(nextNotice.TimeOfDay < DateTime.Now.TimeOfDay)
                        {
                            //次の通知時間が過去の時刻の場合、現在の時刻にプラス○分する
                            nextNotice = DateTime.Now;
                        }
                        //nextNotice += new TimeSpan(0, 1, 0);    //(デバッグ用)1分後
                        nextNotice += new TimeSpan(1, 0, 0);    //1時間後
                        Log.Debug("MyNoticeService", "notice done!! next ; " + nextNotice.ToShortTimeString());
                    }

                    System.Threading.Thread.Sleep(5000);
                }
            });

            t.Start();

            //return base.OnStartCommand(intent, flags, startId);
            return StartCommandResult.Sticky;
        }

        private void SendMail()
        {
            string mailSubject = "終業打刻忘れ通知";
            string mailBody = "終業打刻がされてないっぽいです";

            var addrList = _smtpMailToRaw.Split(";");
            
            var message = new MimeKit.MimeMessage();
            message.From.Add(new MimeKit.MailboxAddress(_smtpAccount));
            foreach (var addr in addrList){
                message.To.Add(new MimeKit.MailboxAddress(addr.Trim()));
            }
            message.Subject = mailSubject;

            var textpart = new MimeKit.TextPart(MimeKit.Text.TextFormat.Plain);
            textpart.Text = mailBody;
            message.Body = textpart;

            using (var client = new MailKit.Net.Smtp.SmtpClient())
            {
                client.ServerCertificateValidationCallback = (s, c, h, e) => true;
                client.Connect(this._smtpServer, this._smtpPort);
                client.Authenticate(_smtpAccount, this._smtpPass);
                client.Send(message);
                client.Disconnect(true);
            }
        }
    }


    /// <summary>
    /// AndroidのサービスからToastを表示するためのクラス
    /// </summary>
    public class LooperThread : Java.Lang.Thread
    {
        private string _msg;

        /// <summary>
        /// コンストラクタ
        /// </summary>
        /// <param name="msg">Toastに表示する文字列を指定</param>
        public LooperThread(string msg)
        {
            this._msg = msg;
        }

        public override void Run()
        {
            //base.Run();
            Looper.Prepare();
            Toast.MakeText(Android.App.Application.Context, _msg, ToastLength.Short).Show();
            Looper.Loop();
        }
    }
}