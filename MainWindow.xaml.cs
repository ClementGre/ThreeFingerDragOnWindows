using System.Drawing;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Interop;
using System.Windows.Media;
using System.Windows.Media.Imaging;

namespace RawInput.Touchpad
{
	public partial class MainWindow : Window
	{

		private ThreeFingersDrag threeFingersDrag;

		public bool TouchpadExists
		{
			get { return (bool)GetValue(TouchpadExistsProperty); }
			set { SetValue(TouchpadExistsProperty, value); }
		}
		public static readonly DependencyProperty TouchpadExistsProperty =
			DependencyProperty.Register("TouchpadExists", typeof(bool), typeof(MainWindow), new PropertyMetadata(false));

		public string TouchpadContacts
		{
			get { return (string)GetValue(TouchpadContactsProperty); }
			set { SetValue(TouchpadContactsProperty, value); }
		}
		public static readonly DependencyProperty TouchpadContactsProperty =
			DependencyProperty.Register("TouchpadContacts", typeof(string), typeof(MainWindow), new PropertyMetadata(null));

		public MainWindow()
		{
			InitializeComponent();
			threeFingersDrag = new ThreeFingersDrag();
		}

		private HwndSource _targetSource;
		private readonly List<string> _log = new();

		protected override void OnSourceInitialized(EventArgs e)
		{
			base.OnSourceInitialized(e);

			_targetSource = PresentationSource.FromVisual(this) as HwndSource;
			_targetSource?.AddHook(WndProc);

			TouchpadExists = TouchpadHelper.Exists();

			_log.Add($"Precision touchpad exists: {TouchpadExists}");

			if (TouchpadExists)
			{
				var success = TouchpadHelper.RegisterInput(_targetSource.Handle);

				_log.Add($"Precision touchpad registered: {success}");
			}
		}

		private IntPtr WndProc(IntPtr hwnd, int msg, IntPtr wParam, IntPtr lParam, ref bool handled)
		{
			switch (msg){
				case TouchpadHelper.WM_INPUT:
					TouchpadContact[] contacts = TouchpadHelper.ParseInput(lParam);
					TouchpadContacts = string.Join(Environment.NewLine, contacts.Select(x => x.ToString()));

					threeFingersDrag.registerTouchpadContacts(contacts);

					_log.Add("---");
					_log.Add(TouchpadContacts);
					break;
			}
			return IntPtr.Zero;
		}

		private void Copy_Click(object sender, RoutedEventArgs e)
		{
			Clipboard.SetText(string.Join(Environment.NewLine, _log));
		}


	}
}




