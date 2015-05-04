//
//  Copyright 2012  Xamarin Inc.
//
//    Licensed under the Apache License, Version 2.0 (the "License");
//    you may not use this file except in compliance with the License.
//    You may obtain a copy of the License at
//
//        http://www.apache.org/licenses/LICENSE-2.0
//
//    Unless required by applicable law or agreed to in writing, software
//    distributed under the License is distributed on an "AS IS" BASIS,
//    WITHOUT WARRANTIES OR CONDITIONS OF ANY KIND, either express or implied.
//    See the License for the specific language governing permissions and
//    limitations under the License.
using System;

using CoreGraphics;
using Foundation;
using UIKit;

using FieldService.Data;
using FieldService.Utilities;
using FieldService.ViewModels;

namespace FieldService.iOS
{
	/// <summary>
	/// Controller for the history section
	/// </summary>
	public partial class HistoryController : BaseController
	{
		readonly HistoryViewModel historyViewModel;
		readonly AssignmentViewModel assignmentViewModel;
		UILabel title;
		TableSource tableSource;

		public HistoryController (IntPtr handle) : base (handle)
		{
			historyViewModel = ServiceContainer.Resolve<HistoryViewModel>();
			assignmentViewModel = ServiceContainer.Resolve<AssignmentViewModel>();
		}

		public override void ViewDidLoad ()
		{
			base.ViewDidLoad ();

			//UI to setup from code
			title = new UILabel (new CGRect (0f, 0f, 160f, 36f)) {
				TextColor = UIColor.White,
				BackgroundColor = UIColor.Clear,
				Font = Theme.BoldFontOfSize (16f),
			};
			var titleButton = new UIBarButtonItem (title);
			
			toolbar.Items = new UIBarButtonItem[] { titleButton };

			tableView.Source = 
				tableSource = new TableSource (this);

			if (Theme.IsiOS7)
				tableView.SeparatorStyle = UITableViewCellSeparatorStyle.SingleLine;
			else
				View.BackgroundColor = Theme.BackgroundColor;
		}

		public override void ViewWillAppear (bool animated)
		{
			base.ViewWillAppear (animated);

			ReloadHIstory ();
		}

		/// <summary>
		/// Reload history
		/// </summary>
		public void ReloadHIstory ()
		{
			if (!IsViewLoaded)
				return;

			var assignment = assignmentViewModel.SelectedAssignment;
			toolbar.SetBackgroundImage (assignment.IsHistory ? Theme.OrangeBar : Theme.BlueBar, UIToolbarPosition.Any, UIBarMetrics.Default);
			tableSource.Enabled = !assignment.IsHistory;

			historyViewModel.LoadHistoryAsync (assignment)
				.ContinueWith (_ => {
					BeginInvokeOnMainThread (() => {
						if (historyViewModel.History == null || historyViewModel.History.Count == 0)
							title.Text = "History";
						else
							title.Text = string.Format ("History ({0})", historyViewModel.History.Count);
						tableView.ReloadData ();
					});
				});
		}

		/// <summary>
		/// Table source for history
		/// </summary>
		class TableSource : UITableViewSource
		{
			readonly HistoryViewModel historyViewModel;
			readonly AssignmentViewModel assignmentViewModel;
			readonly HistoryController controller;
			const string Identifier = "HistoryCell";

			/// <summary>
			/// If true, you can click on the rows
			/// </summary>
			public bool Enabled { get; set; }

			public TableSource (HistoryController controller)
			{
				this.controller = controller;
				historyViewModel = ServiceContainer.Resolve<HistoryViewModel>();
				assignmentViewModel = ServiceContainer.Resolve<AssignmentViewModel>();
			}

			public override nint RowsInSection (UITableView tableview, nint section)
			{
				return historyViewModel.History == null ? 0 : historyViewModel.History.Count;
			}

			public override void RowSelected (UITableView tableView, NSIndexPath indexPath)
			{
				if (!Enabled)
					return;

				var history = historyViewModel.History [indexPath.Row];
				if (history.Type != AssignmentHistoryType.PhoneCall) {
					historyViewModel.LoadAssignmentFromHistory (history)
						.ContinueWith (_ => {
							BeginInvokeOnMainThread (() => {
								var parentController = controller.ParentViewController.ParentViewController;
								assignmentViewModel.LastAssignment = assignmentViewModel.SelectedAssignment;
								assignmentViewModel.SelectedAssignment = historyViewModel.PastAssignment;
								parentController.PerformSegue ("AssignmentHistory", parentController);
							});
						});

					//Deselect the cell, a bug in Apple's UITableView requires BeginInvoke
					BeginInvokeOnMainThread (() => {
						var cell = tableView.CellAt (indexPath);
						cell.SetSelected (false, true);
					});
				}
			}

			public override UITableViewCell GetCell (UITableView tableView, NSIndexPath indexPath)
			{
				var history = historyViewModel.History [indexPath.Row];
				var cell = tableView.DequeueReusableCell (Identifier) as HistoryCell;
				cell.SetHistory (history, Enabled);
				return cell;
			}
		}
	}
}
