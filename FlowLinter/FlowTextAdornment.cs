//------------------------------------------------------------------------------
// <copyright file="FlowTextAdornment.cs" company="Microsoft">
//     Copyright (c) Microsoft.  All rights reserved.
// </copyright>
//------------------------------------------------------------------------------


using System;
using System.Windows.Controls;
using System.Windows.Media;
using Microsoft.VisualStudio.Text;
using Microsoft.VisualStudio.Text.Editor;
using Microsoft.VisualStudio.Text.Formatting;
using System.Threading.Tasks;
using System.Text.RegularExpressions;
using System.IO;
using System.Reflection;
using System.ComponentModel;
using System.IO.Packaging;

namespace FlowLinter
{
	/// <summary>
	/// FlowTextAdornment places red boxes behind all the "a"s in the editor window
	/// </summary>
	internal sealed class FlowTextAdornment
	{
		/// <summary>
		/// The layer of the adornment.
		/// </summary>
		private readonly IAdornmentLayer layer;

		/// <summary>
		/// Text view where the adornment is created.
		/// </summary>
		private readonly IWpfTextView view;

		/// <summary>
		/// Adornment brush.
		/// </summary>
		private readonly Brush brush;

		/// <summary>
		/// Adornment pen.
		/// </summary>
		private readonly Pen pen;
		private ITextDocument document = null;
		private BackgroundWorker backgroundWorker = null;
		private bool flowIsCurrent = false;

		/// <summary>
		/// Initializes a new instance of the <see cref="FlowTextAdornment"/> class.
		/// </summary>
		/// <param name="view">Text view to create the adornment for</param>
		public FlowTextAdornment(IWpfTextView view)
		{
			if (view == null) return;

			backgroundWorker = new BackgroundWorker();
			document = TextAdornmentHelper.GetTextDocument(view.TextBuffer);
			if (document == null || !FileIsJs()) return;

			this.layer = view.GetAdornmentLayer("TextAdornment1");

			this.view = view;
			this.view.LayoutChanged += this.OnLayoutChanged;
			this.view.MouseHover += this.OnMouseHover;
			document.FileActionOccurred += this.OnFileActionOccurred;
			this.backgroundWorker.DoWork += new DoWorkEventHandler(backgroundWorker_DoWork);
			this.backgroundWorker.RunWorkerCompleted += new RunWorkerCompletedEventHandler(backgroundWorker_RunWorkerCompleted);
			// Create the pen and brush to color the box behind the a's
			this.brush = new SolidColorBrush(Color.FromArgb(0x20, 0x00, 0x00, 0xff));
			this.brush.Freeze();

			var penBrush = new SolidColorBrush(Colors.Red);
			penBrush.Freeze();
			this.pen = new Pen(penBrush, 0.5);
			this.pen.Freeze();

			this.backgroundWorker.RunWorkerAsync();
		}

		private void OnMouseHover(object sender, MouseHoverEventArgs e)
		{
			var pos = e.TextPosition;
		}

		private void backgroundWorker_DoWork(object sender, DoWorkEventArgs e)
		{
			try
			{
				if (!FileIsJs() || flowIsCurrent) return;

				FlowRunner.Run(document.FilePath);
				FlowParser.Parse();
				flowIsCurrent = true;
			}
			catch (Exception ex)
			{
				//log exception
			}
		}

		private void backgroundWorker_RunWorkerCompleted(object sender, RunWorkerCompletedEventArgs e)
		{
			try
			{
				if (!FileIsJs()) return;
				layer.RemoveAllAdornments();
				//each key is a line number on which an error has occured
				foreach (var lineNumber in FlowParser.ErrorLineNumbers.Keys)
				{
					//if the error was not in the open document, continue
					if (!document.FilePath.Equals(FlowParser.ErrorLineNumbers[lineNumber].path.Value) || lineNumber < 1) continue;
					var line = view.TextSnapshot.TextBuffer.CurrentSnapshot.GetLineFromLineNumber(lineNumber - 1);
					CreateVisuals(line.Start, line.End);
				}
			}
			catch (Exception ex)
			{
				//log exception
			}
		}

		private bool FileIsJs()
		{
			//check if .js or .jsx file
			Regex regex = new Regex(@"\.jsx?");
			return regex.Match(document.FilePath).Success;
		}

		/// <summary>
		/// Handles whenever the text displayed in the view changes by adding the adornment to any reformatted lines
		/// </summary>
		/// <remarks><para>This event is raised whenever the rendered text displayed in the <see cref="ITextView"/> changes.</para>
		/// <para>It is raised whenever the view does a layout (which happens when DisplayTextLineContainingBufferPosition is called or in response to text or classification changes).</para>
		/// <para>It is also raised whenever the view scrolls horizontally or when its size changes.</para>
		/// </remarks>
		/// <param name="sender">The event sender.</param>
		/// <param name="e">The event arguments.</param>
		internal void OnLayoutChanged(object sender, TextViewLayoutChangedEventArgs e)
		{
			if (!FileIsJs()) return;

			if (document.IsDirty)
				layer.RemoveAllAdornments();
			else if (!backgroundWorker.IsBusy)
				backgroundWorker.RunWorkerAsync();
		}

		internal void OnFileActionOccurred(object sender, TextDocumentFileActionEventArgs e)
		{
			flowIsCurrent = false;
			layer.RemoveAllAdornments();
			if (!backgroundWorker.IsBusy)
				backgroundWorker.RunWorkerAsync();
		}

		/// <summary>
		/// Adds the scarlet box behind the 'a' characters within the given line
		/// </summary>
		/// <param name="line">Line to add the adornments</param>
		private void CreateVisuals(SnapshotPoint start, SnapshotPoint end)
		{
			IWpfTextViewLineCollection textViewLines = this.view.TextViewLines;
			SnapshotSpan span = new SnapshotSpan(this.view.TextSnapshot, Span.FromBounds(start, end));
			Geometry geometry = textViewLines.GetMarkerGeometry(span);

			if (geometry != null)
			{
				var drawing = new GeometryDrawing(this.brush, this.pen, geometry);
				drawing.Freeze();

				var drawingImage = new DrawingImage(drawing);
				drawingImage.Freeze();

				var image = new Image
				{
					Source = drawingImage,
				};

				// Align the image with the top of the bounds of the text geometry
				Canvas.SetLeft(image, geometry.Bounds.Left);
				Canvas.SetTop(image, geometry.Bounds.Top);

				this.layer.AddAdornment(AdornmentPositioningBehavior.TextRelative, span, null, image, null);
			}
		}

	}
}
