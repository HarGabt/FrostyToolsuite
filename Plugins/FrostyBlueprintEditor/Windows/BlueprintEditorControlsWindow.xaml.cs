using System.Diagnostics;
using System.Windows;
using Frosty.Controls;

namespace BlueprintEditorPlugin.Windows
{
    public partial class BlueprintEditorControlsWindow : FrostyDockableWindow
    {
        public BlueprintEditorControlsWindow()
        {
            InitializeComponent();
            ControlsText.Text = "Delete: Removes the selected nodes " +
                                "\nShift + S: Save the current layout" +
                                "\nShift + D: Duplicate the selected nodes" +
                                "\nShift + Right Click: Place the currently selected node in the toolbox at the mouse position" +
                                "\nAlt + Enter: When editing in the property grid, this will apply the edit to all selected nodes." +
                                "\nCtrl + C: Copies the selected node/object in the graph editor to the clipboard" +
                                "\nCtrl + X: Cuts the selected node/object from the graph editor and copies it to the clipboard" +
                                "\nCtrl + V: Pastes the object from the clipboard to the graph editor";

            SearchText.Text = "\"guid:{NodeGuid}\" Searches for a node with a matching guid" +
                              "\n\"fguid:{FileGuid}\" Searches for a node with a matching file guid " +
                              "\n\"hasproperty:{PropertyName}\" Searches for a node with a property of this name" +
                              "\n\"hasvalue:{PropertyName},{Value}\" Searches for a node with a property of this name and value";

            CreditsText.Text = "Emanuel Miroiu - Nodify library used for Graph Editor\n" +
                               "MagixGames - Optimizations to loading Ebx into graphed form\n" +
                               "Mophead01 - Original ObjectFlags calculation implementation\n" +
                               "CosmicDreams - Assistance with determining realms automatically\n" +
                               "GalaxyMan2015 - Original implementation of a BlueprintEditor\n" +
                               "CadeEvs - Graph sorting algorithm\n" +
                               "Y wingpilot2 - Creator of the Blueprint Editor";
        }

        private void GithubButton_OnClick(object sender, RoutedEventArgs e)
        {
            Process.Start("https://github.com/Ywingpilot2/FrostyBlueprintEditor");
        }

        private void OkButton_OnClick(object sender, RoutedEventArgs e)
        {
            Close();
        }
    }
}