#region Imports

using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Resources;
using System.Reflection;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Timers;
using System.Windows.Forms;

using Extensibility;
using EnvDTE;
using EnvDTE80;

using Microsoft.VisualStudio.CommandBars;
using Microsoft.VisualStudio.VCCodeModel; 
#endregion

namespace UpdateUserType
{
	/// <summary>The object for implementing an Add-in.</summary>
	/// <seealso class='IDTExtensibility2' />
	public class Connect : IDTExtensibility2, IDTCommandTarget
    {
        #region Construction

        public Connect ( )
        {
            LoadFileData();
        }
        #endregion

        #region IDTExtensibility2 Members

        /// <summary>Implements the OnConnection method of the IDTExtensibility2 interface. Receives notification that the Add-in is being loaded.</summary>
		/// <param term='application'>Root object of the host application.</param>
		/// <param term='connectMode'>Describes how the Add-in is being loaded.</param>
		/// <param term='addInInst'>Object representing this Add-in.</param>
		/// <seealso class='IDTExtensibility2' />
		public void OnConnection(object application, ext_ConnectMode connectMode, object addInInst, ref Array custom)
		{
			_applicationObject = (DTE2)application;
			_addInInstance = (AddIn)addInInst;
			if(connectMode == ext_ConnectMode.ext_cm_UISetup)
			{
				object []contextGUIDS = new object[] { };
				Commands2 commands = (Commands2)_applicationObject.Commands;
				string toolsMenuName;

				try
				{
					//If you would like to move the command to a different menu, change the word "Tools" to the 
					//  English version of the menu. This code will take the culture, append on the name of the menu
					//  then add the command to that menu. You can find a list of all the top-level menus in the file
					//  CommandBar.resx.
					string resourceName;
					ResourceManager resourceManager = new ResourceManager("UpdateUserType.CommandBar", Assembly.GetExecutingAssembly());
					CultureInfo cultureInfo = new CultureInfo(_applicationObject.LocaleID);
					
					if(cultureInfo.TwoLetterISOLanguageName == "zh")
					{
						System.Globalization.CultureInfo parentCultureInfo = cultureInfo.Parent;
						resourceName = String.Concat(parentCultureInfo.Name, "Tools");
					}
					else
					{
						resourceName = String.Concat(cultureInfo.TwoLetterISOLanguageName, "Tools");
					}
					toolsMenuName = resourceManager.GetString(resourceName);
				}
				catch
				{
					//We tried to find a localized version of the word Tools, but one was not found.
					//  Default to the en-US word, which may work for the current culture.
					toolsMenuName = "Tools";
				}

				//Place the command on the tools menu.
				//Find the MenuBar command bar, which is the top-level command bar holding all the main menu items:
				Microsoft.VisualStudio.CommandBars.CommandBar menuBarCommandBar = ((Microsoft.VisualStudio.CommandBars.CommandBars)_applicationObject.CommandBars)["MenuBar"];

				//Find the Tools command bar on the MenuBar command bar:
				CommandBarControl toolsControl = menuBarCommandBar.Controls[toolsMenuName];
				CommandBarPopup toolsPopup = (CommandBarPopup)toolsControl;
                                
				//This try/catch block can be duplicated if you wish to add multiple commands to be handled by your Add-in,
				//  just make sure you also update the QueryStatus/Exec method to include the new command names.
				try
				{
					//Add a command to the Commands collection:
					Command command = commands.AddNamedCommand2(_addInInstance, "UpdateUserType", "Update User Type", "Updates the Usertype.dat file.", true, 59, ref contextGUIDS, (int)vsCommandStatus.vsCommandStatusSupported+(int)vsCommandStatus.vsCommandStatusEnabled, (int)vsCommandStyle.vsCommandStylePictAndText, vsCommandControlType.vsCommandControlTypeButton);

					//Add a control for the command to the tools menu:
					if((command != null) && (toolsPopup != null))
					{
						command.AddControl(toolsPopup.CommandBar, 1);
					}
				}
				catch(System.ArgumentException)
				{
					//If we are here, then the exception is probably because a command with that name
					//  already exists. If so there is no need to recreate the command and we can 
                    //  safely ignore the exception.
				}

                //This try/catch block can be duplicated if you wish to add multiple commands to be handled by your Add-in,
                //  just make sure you also update the QueryStatus/Exec method to include the new command names.
                try
                {
                    //Add a command to the Commands collection:
                    Command command = commands.AddNamedCommand2(_addInInstance, "MergeUserType", "Configure User Type Merging", "Configures the merging of files into UserTypes.dat.", true, 59, ref contextGUIDS, (int)vsCommandStatus.vsCommandStatusSupported + (int)vsCommandStatus.vsCommandStatusEnabled, (int)vsCommandStyle.vsCommandStylePictAndText, vsCommandControlType.vsCommandControlTypeButton);

                    //Add a control for the command to the tools menu:
                    if ((command != null) && (toolsPopup != null))
                    {
                        command.AddControl(toolsPopup.CommandBar, 1);
                    }
                } catch (System.ArgumentException)
                {
                    //If we are here, then the exception is probably because a command with that name
                    //  already exists. If so there is no need to recreate the command and we can 
                    //  safely ignore the exception.
                }
			}
		}

		/// <summary>Implements the OnDisconnection method of the IDTExtensibility2 interface. Receives notification that the Add-in is being unloaded.</summary>
		/// <param term='disconnectMode'>Describes how the Add-in is being unloaded.</param>
		/// <param term='custom'>Array of parameters that are host application specific.</param>
		/// <seealso class='IDTExtensibility2' />
		public void OnDisconnection(ext_DisconnectMode disconnectMode, ref Array custom)
		{
            StopMonitoring();
		}

		/// <summary>Implements the OnAddInsUpdate method of the IDTExtensibility2 interface. Receives notification when the collection of Add-ins has changed.</summary>
		/// <param term='custom'>Array of parameters that are host application specific.</param>
		/// <seealso class='IDTExtensibility2' />		
		public void OnAddInsUpdate(ref Array custom)
		{
		}

		/// <summary>Implements the OnStartupComplete method of the IDTExtensibility2 interface. Receives notification that the host application has completed loading.</summary>
		/// <param term='custom'>Array of parameters that are host application specific.</param>
		/// <seealso class='IDTExtensibility2' />
		public void OnStartupComplete(ref Array custom)
		{
		}

		/// <summary>Implements the OnBeginShutdown method of the IDTExtensibility2 interface. Receives notification that the host application is being unloaded.</summary>
		/// <param term='custom'>Array of parameters that are host application specific.</param>
		/// <seealso class='IDTExtensibility2' />
		public void OnBeginShutdown(ref Array custom)
		{
		}
		
		/// <summary>Implements the QueryStatus method of the IDTCommandTarget interface. This is called when the command's availability is updated</summary>
		/// <param term='commandName'>The name of the command to determine state for.</param>
		/// <param term='neededText'>Text that is needed for the command.</param>
		/// <param term='status'>The state of the command in the user interface.</param>
		/// <param term='commandText'>Text requested by the neededText parameter.</param>
		/// <seealso class='Exec' />
		public void QueryStatus(string commandName, vsCommandStatusTextWanted neededText, ref vsCommandStatus status, ref object commandText)
		{
			if(neededText == vsCommandStatusTextWanted.vsCommandStatusTextWantedNone)
			{
				if ((commandName == "UpdateUserType.Connect.UpdateUserType") ||
                    (commandName == "UpdateUserType.Connect.MergeUserType"))
				{
					status = (vsCommandStatus)vsCommandStatus.vsCommandStatusSupported|vsCommandStatus.vsCommandStatusEnabled;
					return;
				}
			}
		}

		/// <summary>Implements the Exec method of the IDTCommandTarget interface. This is called when the command is invoked.</summary>
		/// <param term='commandName'>The name of the command to execute.</param>
		/// <param term='executeOption'>Describes how the command should be run.</param>
		/// <param term='varIn'>Parameters passed from the caller to the command handler.</param>
		/// <param term='varOut'>Parameters passed from the command handler to the caller.</param>
		/// <param term='handled'>Informs the caller if the command was handled or not.</param>
		/// <seealso class='Exec' />
		public void Exec(string commandName, vsCommandExecOption executeOption, ref object varIn, ref object varOut, ref bool handled)
		{
			handled = false;
			if(executeOption == vsCommandExecOption.vsCommandExecOptionDoDefault)
			{
				if(commandName == "UpdateUserType.Connect.UpdateUserType")
				{
                    UpdateUserTypeFile();
					handled = true;
					return;
                } else if (commandName == "UpdateUserType.Connect.MergeUserType")
                {
                    MergeUserTypes();
                    handled = true;
                    return;
                };
			}
        }
        #endregion

        #region Private Members

        #region Attributes

        private string ConfigurationFile
        {
            get
            {
                Assembly asm = Assembly.GetExecutingAssembly();
                return Path.Combine(Path.GetDirectoryName(asm.Location), "UpdateUserType.xml");
            }
        }

        private string UserTypeFile
        {
            get
            {
                if (m_strUserTypeFile == null)
                {
                    //Get the path to the IDE
                    m_strUserTypeFile = Path.GetDirectoryName(_applicationObject.FullName);
                    m_strUserTypeFile = Path.Combine(m_strUserTypeFile, "UserType.dat");
                };

                return m_strUserTypeFile ?? "";
            }

        }
        #endregion

        #region Methods

        //Runs on background thread
        private bool AppendFile ( StreamWriter writer, string filename )
        {
            if (File.Exists(filename))
            {
                try
                {
                    using (StreamReader reader = File.OpenText(filename))
                    {
                        string contents = reader.ReadToEnd();
                        writer.Write(contents);
                        writer.WriteLine();
                    };

                    return true;
                } catch (IOException e)
                {
                    Log("Error trying to append file '{0}' to user types file - {1}", filename, e.Message);
                    return false;
                };
            };

            return true;
        }

        //Runs on UI thread
        private bool ConfigureUserTypes ( )
        {
            //Display the configuration UI
            MergeFilesDialog dlg = new MergeFilesDialog();

            lock (m_configuration)
            {
                //Set up the files that should be monitored by default
                foreach (FileData data in m_configuration.MonitorFiles)
                    dlg.MonitorFiles.Add(data);
            };
            dlg.AutoRefresh = m_configuration.AutoRefresh;
            dlg.RefreshInterval = m_configuration.RefreshInterval;
            if (dlg.ShowDialog() != DialogResult.OK)
                return false;

            lock (m_configuration)
            {
                //Update the list of files being monitored
                StopMonitoring();
                try
                {
                    m_configuration.AutoRefresh = dlg.AutoRefresh;
                    m_configuration.RefreshInterval = dlg.RefreshInterval;

                    m_configuration.MonitorFiles.Clear();
                    m_configuration.MonitorFiles.AddRange(dlg.MonitorFiles);

                    //Save the changes
                    m_configuration.Save(ConfigurationFile);       
                } catch (Exception e)
                {
                    MessageBox.Show(e.Message, "Error saving configuration", MessageBoxButtons.OK, MessageBoxIcon.Error);
                    Log("Error saving configuration - {0}", e.Message);
                    return false;
                } finally
                {
                    StartMonitoring();
                };
            };

            //Force an update
            UpdateUserTypeFile();

            return true;
        }

        private bool FilesHaveChanged ( )
        {
            lock (m_configuration)
            {
                var files = from file in m_configuration.MonitorFiles
                            where file.Monitor
                            select file;
                foreach (FileData file in files)
                {
                    DateTime dtLast = File.GetLastWriteTimeUtc(file.FullPath);
                    if (dtLast > file.LastWriteTime)
                        return true;
                };

                return false;
            };
        }
       
        //Runs on UI thread
        private void LoadFileData ( )
        {
            try
            {
                lock (m_configuration)
                {
                    m_configuration.Load(ConfigurationFile);                

                    StartMonitoring();
                };
            } catch (Exception e)
            {
                MessageBox.Show(e.Message, "UpdateUserType - Configuration Corrupt", MessageBoxButtons.OK, MessageBoxIcon.Error);
                Log("Configuration is corrupt - {0}", e.Message);
            };
        }

        private void Log ( string message, params object[] arguments )
        {
            OutputWindow wnd = _applicationObject.ToolWindows.OutputWindow;
            if (wnd != null)
            {
                OutputWindowPane pane = wnd.OutputWindowPanes.Item("UpdateUserType");
                if (pane == null)
                    pane = wnd.OutputWindowPanes.Add("UpdateUserType");

                pane.OutputString(String.Format(message, arguments));
            };
        }

        //Runs on background thread
        private void MergeUserTypes ( )
        {               
            if (ConfigureUserTypes())
                UpdateUserTypeFile();
        }        

        //Runs on background thread
        private void NotifyVisualStudio ( )
        {
            VCLanguageManager mgr = _applicationObject.GetObject("VCLanguageManager") as VCLanguageManager;
            if (mgr != null)
            {
                mgr.RefreshUserKeywords("");
            };
        }

        private void StartMonitoring ( )
        {
            if (m_configuration.AutoRefresh && (m_configuration.RefreshInterval.TotalMilliseconds > 0))
            {
                m_timer = new System.Timers.Timer(m_configuration.RefreshInterval.TotalMilliseconds);
                m_timer.Elapsed += OnTimer;
                m_timer.AutoReset = true;
                m_timer.Start();
            };
        }

        private void StopMonitoring ( )
        {
            if (m_timer != null)
            {
                m_timer.Stop();
                m_timer = null;
            };
        }

        //Runs on any thread
        private void UpdateUserTypeFile ( )
        {
            //Create a temporary file
            string filename = Path.GetTempFileName();
            try
            {
                lock (m_configuration)
                {
                    var files = from file in m_configuration.MonitorFiles
                                where file.Monitor
                                select file;

                    using (FileStream stream = File.OpenWrite(filename))
                    {
                        using (StreamWriter writer = new StreamWriter(stream))
                        {
                            //For each file being monitored                            
                            foreach (FileData file in files)
                            {
                                writer.WriteLine(";;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;");
                                writer.WriteLine(";; " + file.FullPath);
                                writer.WriteLine(";;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;");
                                writer.WriteLine();
                                AppendFile(writer, file.FullPath);
                                writer.WriteLine();
                                writer.WriteLine(";;");
                                writer.WriteLine(";;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;");
                                writer.WriteLine();
                            };
                        };
                    };

                    //Now replace the original
                    File.Delete(UserTypeFile);
                    File.Move(filename, UserTypeFile);

                    //Update the last write time so we won't grab these files again
                    DateTime dtNow = DateTime.UtcNow;
                    foreach (FileData file in files)
                    {
                        file.LastWriteTime = dtNow;
                    };
                };

                NotifyVisualStudio();               
            } catch (Exception e)
            {
                Log("Error merging files - {0}", e.Message);
            } finally
            {
                try
                {
                    if (File.Exists(filename))
                        File.Delete(filename);
                } catch
                { /* Ignore */ };
            };
        }        
        #endregion

        #region Event Handlers

        private void OnTimer ( object sender, ElapsedEventArgs e )
        {
            //Pause the timer just in case we take too long
            System.Timers.Timer tmr = sender as System.Timers.Timer;
            if (tmr != null)
                tmr.Enabled = false;

            try
            {
                //Determine if any files have changed
                if (FilesHaveChanged())
                    UpdateUserTypeFile();
            } finally
            {
                if (tmr != null)
                    tmr.Enabled = true;
            };
        }
        #endregion

        #region Data

        private DTE2 _applicationObject;
        private AddIn _addInInstance;

        private string m_strUserTypeFile;

        //Take a lock on this before making any changes to it
        private ConfigurationReaderWriter m_configuration = new ConfigurationReaderWriter();
        private System.Timers.Timer m_timer;
        #endregion
                
        #endregion 
    }
}