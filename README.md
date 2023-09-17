# favorite-assets-panel

Provides an easy access to your favorite Unity assets. Featuring a built-in system for saving/loading assets lists for backup, syncing projects across multiple computers or quickly switching different projects/tasks.

## Installation
Clone the project to your computer and add a sym-link to it on your project. For example:
```
git clone git@github.com:yuvalKooply/favorite-assets-panel.git /Users/yuval/Development/Apps/favorite-assets-panel
```

Then add the sym-link:
```
ln -s /Users/yuval/Development/Apps/favorite-assets-panel /Users/yuval/Development/Apps/g-01/Assets/Editor/_private
```

## Usage

Add the panel via "Window/Panels/Favorites". Now pick an asset you want an easy access to from your Project panel, right click and select the ♥. Go back to the Favorites panel and you should see it on the list. Click it to quickly access it or click Select to select it on the Projects panel. Click Edit for more options.

To remove an asset from the list, click the Edit button next to it and then click the "X" button on the other side.

The panel will use EditorPrefs as default, but it's recommended to use data files instead since they're a lot less volatile. If you use a data file, your assets list will be written to disk as a file so you can make backups or switch between different lists. To do so, click "Menu", "Create Data File" and pick a location and filename for your data file. Now the panel should write directly to it.

Notice that the panel should rememeber the GUID of your asssets so you can freely move them around, however, after moving a favorite asset you might need to click it twice in the list since it'll recognize the asset has been moved only after the first click. Alternatively, you can just click the "Refresh" button to refresh the entire list.

Special assets support:
* Prefabs - click to open them for edit in Unity
* Folders - click to open the folder in the Project panel
