//------------------------------------------------------------------------------
// <copyright file="TextAdornmentHelper.cs" company="Microsoft">
//     Copyright (c) Microsoft.  All rights reserved.
// </copyright>
//------------------------------------------------------------------------------

using System;
using System.Windows.Controls;
using System.Windows.Media;
using Microsoft.VisualStudio.Text;
using Microsoft.VisualStudio.Text.Editor;
using Microsoft.VisualStudio.Text.Formatting;

namespace FlowLinter
{
	/// <summary>
	/// TextAdornmentHelper places red boxes behind all the "a"s in the editor window
	/// </summary>
	public static class TextAdornmentHelper
	{

		public static ITextDocument GetTextDocument(this ITextBuffer TextBuffer)
		{
			ITextDocument textDoc;
			var rc = TextBuffer.Properties.TryGetProperty<ITextDocument>(
			  typeof(ITextDocument), out textDoc);
			if (rc == true)
				return textDoc;
			else
				return null;
		}

	}
}
