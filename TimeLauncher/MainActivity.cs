using Android.App;
using Android.Content;
using Android.Content.PM;
using Android.Graphics.Drawables;
using Android.OS;
using Android.Runtime;
using Android.Support.V7.App;
using Android.Widget;
using System;
using System.Timers;
using Xamarin.Essentials;
using Android.Util;

namespace TimeLauncher
{
    [Activity(Label = "@string/app_name", Theme = "@style/AppTheme", MainLauncher = true)]
    public class MainActivity : AppCompatActivity
    {
        private DateTime displayTime;
        private string launchApp_LabelName;
        private string launchApp_className;
        private string launchApp_pkgName;
        private string noticemail_smtp;
        private string noticemail_pass;
        private string noticemail_port;
        private string noticemail_account;
        private string noticemail_mailto;

        private bool serviceRunning;


        private MyReceiver myReceiver = new MyReceiver();
        //private Timer timer;
        //private bool timer_Notice_finish;

        protected override void OnCreate(Bundle savedInstanceState)
        {
            base.OnCreate(savedInstanceState);
            Xamarin.Essentials.Platform.Init(this, savedInstanceState);
            // Set our view from the "main" layout resource
            SetContentView(Resource.Layout.activity_main);

            // アイコンの取得先 https://www.iconfinder.com/iconsets/moe-moe-icons
            //保存した情報を読み取り、メンバに保存
            var saveTime = this.GetSharedPreferences("app", FileCreationMode.Private);
            launchApp_LabelName = saveTime.GetString("LabelName", "(起動するアプリが選択されていません)");
            launchApp_className = saveTime.GetString("ClassName", "");
            launchApp_pkgName = saveTime.GetString("PkgName", "");
            saveTime = this.GetSharedPreferences("time", FileCreationMode.Private);
            var tmpTime = saveTime.GetString("time", "00:00");
            displayTime = DateTime.Parse(tmpTime);
            saveTime = this.GetSharedPreferences("smtp", FileCreationMode.Private);
            noticemail_smtp = saveTime.GetString("smtp_host", "initial SMTP");
            noticemail_pass = saveTime.GetString("smtp_pass", "initiap PASS");
            noticemail_port = saveTime.GetString("smtp_port", "0");
            noticemail_account = saveTime.GetString("smtp_account", "intial ACCOUNT");
            noticemail_mailto = saveTime.GetString("smtp_mailto", "intial MAILTO");

            //サービスの動作状況を示す変数初期化(サービス二重起動防止)
            serviceRunning = false;

            // 画面表示の設定
            TextView textView2 = FindViewById<TextView>(Resource.Id.textView2);
            TextView textView1 = FindViewById<TextView>(Resource.Id.textView1);
            ImageView imageView = FindViewById<ImageView>(Resource.Id.appIcon);
            textView2.Text = displayTime.ToShortTimeString() + " 以降に起動可能";
            textView1.Text = launchApp_LabelName;
            imageView.SetImageDrawable(this.getIconfromClassName(launchApp_className));

            // 各種ボタンのイベントハンドラ作成
            var btn_showSelectTime = FindViewById<Button>(Resource.Id.btn_showSelectTime);
            var btn_launch = FindViewById<Button>(Resource.Id.btn_Launch);
            var btn_showSelectApp = FindViewById<Button>(Resource.Id.btn_showSelectApp);
            btn_showSelectTime.Click += btn_showSelectTime_Click;
            btn_showSelectApp.Click += Btn_showSelectApp_Click;
            btn_launch.Click += Btn_launch_Click;

            // 指定した時刻で通知するタイマの初期化
            // サービスで打刻時刻の通知を行うため、無効化
            //timer_Notice_finish = false;
            //timer = new Timer(1000)
            //{
            //    Enabled = true
            //};
            //timer.Elapsed += Timer_Elapsed;
            //timer.Start();
        }

        // サービスで打刻自国の通知を行うため無効化
        //private void Timer_Elapsed(object sender, ElapsedEventArgs e)
        //{
        //    // 現在時刻と指定時刻の差分(指定時刻超過でプラスになる)
        //    TimeSpan ts = DateTime.Now.TimeOfDay - displayTime.TimeOfDay;

        //    if (ts < new TimeSpan(0, 0, 0))
        //    {
        //        timer_Notice_finish = false;
        //    }
        //    if (!timer_Notice_finish && ts >= new TimeSpan(0, 0, 0) && ts < new TimeSpan(0, 0, 5) )
        //    {
        //        timer_Notice_finish = true;
        //        // timerでToastを呼ぶのは相性悪いっぽい(実行されない)。Toastの次のtryに行く前に終わる
        //        //Toast.MakeText(this, "指定時刻です", ToastLength.Short).Show();
        //        try
        //        {
        //            Vibration.Vibrate(1000);
        //        }
        //        catch (FeatureNotSupportedException)
        //        {
        //            // バイブしなくても致命的な問題ではないので、例外処理は何もしない
        //        }
        //    }
        //}

        protected override void OnPause()
        {
            base.OnPause();

            //http://y-anz-m.blogspot.com/2011/08/androidkeygurad.html
            IntentFilter f = new IntentFilter();
            f.AddAction(Intent.ActionUserPresent);
            RegisterReceiver(myReceiver, f);
        }
        protected override void OnResume()
        {
            base.OnResume();

            if (myReceiver.IsOrderedBroadcast)
            {
                UnregisterReceiver(myReceiver);
            }
        }

        protected override void OnDestroy()
        {
            base.OnDestroy();
            if (myReceiver.IsOrderedBroadcast)
            {
                UnregisterReceiver(myReceiver);
            }

            //timer.Stop();
        }


        private void Btn_showSelectApp_Click(object sender, EventArgs e)
        {
            var intent = new Intent(this, typeof(AppSelectActivity));
            int requestCode = 2000;
            StartActivityForResult(intent, requestCode);
        }

        private void Btn_launch_Click(object sender, EventArgs e)
        {
            // 二重起動防止フラグを確認して通知サービスへデータを渡して起動
            // もうちょっとちゃんとした方法がいいけど、getRunningServices は古くてダメっぽい
            if (!serviceRunning)
            {
                string[] put_string_array =
                {
                    displayTime.ToShortTimeString(),
                    noticemail_mailto,
                    noticemail_smtp,
                    noticemail_port,
                    noticemail_account,
                    noticemail_pass,  
                };
                var myServiceIntent = new Intent(this, typeof(MyNoticeService));
                myServiceIntent.PutExtra(MyNoticeService.EXTRA_NAME, put_string_array);
                StartService(myServiceIntent);
                serviceRunning = true;
            }
            else
            {
                Log.Debug("MainActivity", "二重起動防止！");
            }

            // 時刻の比較
            if (DateTime.Now.TimeOfDay < displayTime.TimeOfDay)
            {
                try
                {
                    Vibration.Vibrate();
                }
                catch (FeatureNotSupportedException)
                {
                    // バイブしてもしなくても影響はないので例外を無視
                }
                Toast.MakeText(this, "まだ時間になっていません", ToastLength.Short).Show();
                return;
            }

            //15時以降にアプリ起動した場合、終業打刻を行ったものとして扱う
            MyNoticeService.evningDakokuDone = DateTime.Now.TimeOfDay > new TimeSpan(15, 0, 0);
            //MyNoticeService.evningDakokuDone = DateTime.Now.TimeOfDay > new TimeSpan(10, 17, 0); // debug用

            // 選択済みのアプリ起動
            if (string.IsNullOrEmpty(launchApp_pkgName)) return;
            var intent = Intent;
            intent.SetClassName(launchApp_pkgName, launchApp_className);
            StartActivity(intent);
        }

        private void btn_showSelectTime_Click(object sender, System.EventArgs e)
        {
            // 遷移先の画面にデータを渡す
            var intent = new Intent(this, typeof(TimeSelectActivity));
            intent.PutExtra("EXTRA_DATA", displayTime.ToString("HH:mm"));
            intent.PutExtra("EXTRA_SMTP", noticemail_smtp);
            intent.PutExtra("EXTRA_PASS", noticemail_pass);
            intent.PutExtra("EXTRA_PORT", noticemail_port);
            intent.PutExtra("EXTRA_ACCOUNT", noticemail_account);
            intent.PutExtra("EXTRA_MAILTO", noticemail_mailto);

            int requestCode = 1000;
            //StartActivity(intent);
            StartActivityForResult(intent, requestCode);    // 遷移先から値を受け取りたいからこっち
        }

        protected override void OnActivityResult(int requestCode, [GeneratedEnum] Result resultCode, Intent data)
        {
            base.OnActivityResult(requestCode, resultCode, data);

            if (resultCode != Result.Ok || null == data) return;
            
            // TimePickerから値を受け取り、TextViewに表示する
            if (requestCode == 1000)
            {
                noticemail_smtp = data.GetStringExtra("EXTRA_SMTP");
                noticemail_pass = data.GetStringExtra("EXTRA_PASS");
                noticemail_port = data.GetStringExtra("EXTRA_PORT");
                noticemail_account = data.GetStringExtra("EXTRA_ACCOUNT");
                noticemail_mailto = data.GetStringExtra("EXTRA_MAILTO");

                string res = data.GetStringExtra("EXTRA_DATA");
                displayTime = DateTime.Parse(res);
                TextView textView2 = FindViewById<TextView>(Resource.Id.textView2);
                textView2.Text = displayTime.ToShortTimeString() + " 以降に起動可能";

                // 受け取った時刻を保存する
                var saveTime = this.GetSharedPreferences("time", FileCreationMode.Private);
                var editor = saveTime.Edit();
                editor.PutString("time", displayTime.ToShortTimeString());
                editor.Commit();

                saveTime = this.GetSharedPreferences("smtp", FileCreationMode.Private);
                editor = saveTime.Edit();
                editor.PutString("smtp_host", noticemail_smtp);
                editor.PutString("smtp_pass", noticemail_pass);
                editor.PutString("smtp_port", noticemail_port);
                editor.PutString("smtp_account", noticemail_account);
                editor.PutString("smtp_mailto", noticemail_mailto);
                editor.Commit();
            }

            // アプリ選択から値を受け取り、画面に表示
            if(requestCode == 2000){
                launchApp_LabelName = data.GetStringExtra("EXTRA_LABEL");
                launchApp_className = data.GetStringExtra("EXTRA_CLSNAME");
                launchApp_pkgName = data.GetStringExtra("EXTRA_PKGNAME");

                TextView textView = FindViewById<TextView>(Resource.Id.textView1);
                textView.Text = launchApp_LabelName;

                ImageView imageView = FindViewById<ImageView>(Resource.Id.appIcon);
                imageView.SetImageDrawable(this.getIconfromClassName(launchApp_className));

                // 値を保存
                var saveAppInfo = this.GetSharedPreferences("app", FileCreationMode.Private);
                var editor = saveAppInfo.Edit();
                editor.PutString("LabelName", launchApp_LabelName);
                editor.PutString("ClassName", launchApp_className);
                editor.PutString("PkgName", launchApp_pkgName);
                editor.Commit();
            }
        }

        /// <summary>
        /// インストール済みアプリのクラス名を渡して、アプリのアイコンのDrawableを取得する
        /// </summary>
        /// <param name="className"></param>
        /// <returns></returns>
        private Drawable getIconfromClassName(string className)
        {
            var mainIntent = new Intent(Intent.ActionMain, null);
            mainIntent.AddCategory(Intent.CategoryLauncher);
            var ar = PackageManager.QueryIntentActivities(mainIntent, 0);
            foreach (var a in ar)
            {
                if (a.ActivityInfo.Name == className)
                {
                    return a.ActivityInfo.LoadIcon(PackageManager);
                }
            }
            return null;
        }

        public override void OnRequestPermissionsResult(int requestCode, string[] permissions, [GeneratedEnum] Android.Content.PM.Permission[] grantResults)
        {
            Xamarin.Essentials.Platform.OnRequestPermissionsResult(requestCode, permissions, grantResults);

            base.OnRequestPermissionsResult(requestCode, permissions, grantResults);
        }

    }


    public class MyReceiver : BroadcastReceiver
    {
        public override void OnReceive(Context context, Intent intent)
        {
            string action = intent.Action;

            if(action == Intent.ActionUserPresent)
            {
                //ロック解除イベント発生時、アプリを起動(前面に表示)させる
                Toast.MakeText(context, "ロック解除のイベント検知", ToastLength.Short).Show();
                context.StartActivity(typeof(MainActivity));
            }
        }
    }
}