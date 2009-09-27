using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;

namespace EpubMaker
{
	/// <summary>
	/// Interaction logic for MainWindow.xaml
	/// </summary>
	public partial class MainWindow : Window
	{
		private readonly Page[] pages = {new Source(), new Metadata(), new Toc(), new Style(), new Finish()};
		private int pageNo = 0;
		private BookInfo bookInfo = new BookInfo();

		public MainWindow()
		{
			InitializeComponent();

			foreach (Page page in pages)
			{
				((IStep) page).StateChanged += OnPageStateChanged;
			}

			Background = SystemColors.ControlLightBrush;
			NextPage();
		}

		private Page currentPage;
		private States currentState = States.Ready;

		private void NextPage()
		{
			currentPage = pages[pageNo++];
			var currentStep = (IStep) currentPage;

			currentStep.Init(bookInfo);
			pagesHolder.Navigate(currentPage);

			if (pageNo == pages.Length)
			{
				btnFinish.Visibility = Visibility.Hidden;
				btnNext.Content = "Finish";
				btnNext.IsEnabled = true;
			}
			if (currentStep.CanProceed)
			{
				btnNext.IsEnabled = true;
			}
			else
			{
				status.Content = "Please fill in the required information";
			}

			if (pageNo > 1)
			{
				btnBack.Visibility = Visibility.Visible;
			}
		}

		void OnPageStateChanged(IStep sender, StateChangeArgs newState)
		{
			currentState = newState.State;
			status.Content = newState.Description;

			switch (newState.State)
			{
				case States.Working:
					btnNext.IsEnabled = false;
					btnFinish.IsEnabled = false;
					break;
				case States.Ready:
					btnNext.IsEnabled = true;
					btnFinish.IsEnabled = true;
					break;
			}
		}

		private void btnNext_Click(object sender, RoutedEventArgs e)
		{
			try
			{
				Forward();
			}
			catch (Exception ex)
			{
				MessageBox.Show(ex.Message, "Error", MessageBoxButton.OK, MessageBoxImage.Error);
			}
		}

		private bool Forward()
		{
			try
			{
				((IStep)currentPage).Wrapup(bookInfo);

				if (pageNo == pages.Length)
				{
					Application.Current.Shutdown();
					return false;
				}
				else if (currentState == States.Ready) // Give them a chance to repeat the step
				{
					NextPage();
				}
				else
				{
					return false;
				}

				return true;
			}
			finally
			{
				Cursor = Cursors.Arrow;
				Mouse.OverrideCursor = null;
			}
		}

		private void btnBack_Click(object sender, RoutedEventArgs e)
		{
			Back();
		}

		private void Back()
		{
			pagesHolder.Navigate(pages[--pageNo]);
			if (pageNo == 0)
			{
				btnBack.Visibility = Visibility.Hidden;
			}
			btnNext.Content = "Next >";
			btnFinish.Visibility = Visibility.Visible;
		}

		private void btnFinish_Click(object sender, RoutedEventArgs e)
		{
			try
			{
				while (Forward())
				{
				}
			}
			catch (Exception ex)
			{
				MessageBox.Show(ex.Message, "Error", MessageBoxButton.OK, MessageBoxImage.Error);
			}
		}
	}
}
