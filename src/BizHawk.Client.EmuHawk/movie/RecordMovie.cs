﻿using System;
using System.Drawing;
using System.IO;
using System.Windows.Forms;
using System.Linq;

using BizHawk.Emulation.Common;
using BizHawk.Client.Common;
using BizHawk.Common;

namespace BizHawk.Client.EmuHawk
{
	// TODO - Allow relative paths in record TextBox
	public partial class RecordMovie : Form, IDialogParent
	{
		private readonly IMainFormForTools _mainForm;
		private readonly Config _config;
		private readonly GameInfo _game;
		private readonly IEmulator _emulator;
		private readonly IMovieSession _movieSession;
		private readonly FirmwareManager _firmwareManager;

		public IDialogController DialogController => _mainForm;

		public RecordMovie(
			IMainFormForTools mainForm,
			Config config,
			GameInfo game,
			IEmulator core,
			IMovieSession movieSession,
			FirmwareManager firmwareManager)
		{
			if (game.IsNullInstance()) throw new InvalidOperationException("how is the traditional Record dialog open with no game loaded? please report this including as much detail as possible");

			_mainForm = mainForm;
			_config = config;
			_game = game;
			_emulator = core;
			_movieSession = movieSession;
			_firmwareManager = firmwareManager;
			InitializeComponent();
			Icon = Properties.Resources.TAStudioIcon;
			BrowseBtn.Image = Properties.Resources.OpenFile;
			if (OSTailoredCode.IsUnixHost) Load += (_, _) =>
			{
				//HACK to make this usable on Linux. No clue why this Form in particular is so much worse, maybe the GroupBox? --yoshi
				groupBox1.Height -= 24;
				DefaultAuthorCheckBox.Location += new Size(0, 32);
				var s = new Size(0, 40);
				OK.Location += s;
				Cancel.Location += s;
			};

			if (!_emulator.HasSavestates())
			{
				StartFromCombo.Items.Remove(
					StartFromCombo.Items
						.OfType<object>()
						.First(i => i.ToString()
							.ToLower() == "now"));
			}

			if (!_emulator.HasSaveRam())
			{
				StartFromCombo.Items.Remove(
					StartFromCombo.Items
						.OfType<object>()
						.First(i => i.ToString()
							.ToLower() == "saveram"));
			}
		}

		private string MakePath()
		{
			var path = RecordBox.Text;

			if (!string.IsNullOrWhiteSpace(path))
			{
				if (path.LastIndexOf(Path.DirectorySeparatorChar) == -1)
				{
					if (path[0] != Path.DirectorySeparatorChar)
					{
						path = path.Insert(0, Path.DirectorySeparatorChar.ToString());
					}

					path = _config.PathEntries.MovieAbsolutePath() + path;

					if (!MovieService.MovieExtensions.Contains(Path.GetExtension(path)))
					{
						// If no valid movie extension, add movie extension
						path += $".{MovieService.StandardMovieExtension}";
					}
				}
			}

			return path;
		}

		private void Ok_Click(object sender, EventArgs e)
		{
			var path = MakePath();
			if (!string.IsNullOrWhiteSpace(path))
			{
				var test = new FileInfo(path);
				if (test.Exists)
				{
					var result = DialogController.ShowMessageBox2($"{path} already exists, overwrite?", "Confirm overwrite", EMsgBoxIcon.Warning, useOKCancel: true);
					if (!result)
					{
						return;
					}
				}

				var movieToRecord = _movieSession.Get(path);

				var fileInfo = new FileInfo(path);
				if (!fileInfo.Exists)
				{
					Directory.CreateDirectory(fileInfo.DirectoryName);
				}

				if (StartFromCombo.SelectedItem.ToString() == "Now" && _emulator.HasSavestates())
				{
					var core = _emulator.AsStatable();

					movieToRecord.StartsFromSavestate = true;

					if (_config.Savestates.Type == SaveStateType.Binary)
					{
						movieToRecord.BinarySavestate = core.CloneSavestate();
					}
					else
					{
						using var sw = new StringWriter();
						core.SaveStateText(sw);
						movieToRecord.TextSavestate = sw.ToString();
					}

					// TODO: do we want to support optionally not saving this?
					movieToRecord.SavestateFramebuffer = Array.Empty<int>();
					if (_emulator.HasVideoProvider())
					{
						movieToRecord.SavestateFramebuffer = (int[])_emulator.AsVideoProvider().GetVideoBuffer().Clone();
					}
				}
				else if (StartFromCombo.SelectedItem.ToString() == "SaveRam"  && _emulator.HasSaveRam())
				{
					var core = _emulator.AsSaveRam();
					movieToRecord.StartsFromSaveRam = true;
					movieToRecord.SaveRam = core.CloneSaveRam();
				}

				movieToRecord.PopulateWithDefaultHeaderValues(
					_emulator,
					((MainForm) _mainForm).GetSettingsAdapterForLoadedCoreUntyped(), //HACK
					_game,
					_firmwareManager,
					AuthorBox.Text ?? _config.DefaultAuthor);
				movieToRecord.Save();
				_mainForm.StartNewMovie(movieToRecord, true);

				_config.UseDefaultAuthor = DefaultAuthorCheckBox.Checked;
				if (DefaultAuthorCheckBox.Checked)
				{
					_config.DefaultAuthor = AuthorBox.Text;
				}

				Close();
			}
			else
			{
				DialogController.ShowMessageBox("Please select a movie to record", "File selection error", EMsgBoxIcon.Error);
			}
		}

		private void Cancel_Click(object sender, EventArgs e)
		{
			Close();
		}

		private void BrowseBtn_Click(object sender, EventArgs e)
		{
			string movieFolderPath = _config.PathEntries.MovieAbsolutePath();
			
			// Create movie folder if it doesn't already exist
			try
			{
				Directory.CreateDirectory(movieFolderPath);
			}
			catch (Exception movieDirException)
			{
				if (movieDirException is IOException
					|| movieDirException is UnauthorizedAccessException)
				{
					//TO DO : Pass error to user?
				}
				else throw;
			}
			
			var filterset = _movieSession.Movie.GetFSFilterSet();
			var result = this.ShowFileSaveDialog(
				fileExt: $".{filterset.Filters[0].Extensions.First()}",
				filter: filterset,
				initDir: movieFolderPath,
				initFileName: RecordBox.Text,
				muteOverwriteWarning: true);
			if (!string.IsNullOrWhiteSpace(result)) RecordBox.Text = result;
		}

		private void RecordMovie_Load(object sender, EventArgs e)
		{
			RecordBox.Text = _game.FilesystemSafeName();
			StartFromCombo.SelectedIndex = 0;
			DefaultAuthorCheckBox.Checked = _config.UseDefaultAuthor;
			if (_config.UseDefaultAuthor)
			{
				AuthorBox.Text = _config.DefaultAuthor;
			}
		}

		private void RecordBox_DragEnter(object sender, DragEventArgs e)
		{
			e.Set(DragDropEffects.Copy);
		}

		private void RecordBox_DragDrop(object sender, DragEventArgs e)
		{
			var filePaths = (string[])e.Data.GetData(DataFormats.FileDrop);
			RecordBox.Text = filePaths[0];
		}
	}
}
