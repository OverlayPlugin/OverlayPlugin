# Updating to OverlayPlugin Fork

tl;dr: OverlayPlugin will break in 6.2 and will require all users to perform manual steps listed below to update it.

## Details

OverlayPlugin is moving from <https://github.com/ngld/OverlayPlugin/> to <https://github.com/OverlayPlugin/OverlayPlugin>.
Unfortunately, nobody has a way to make a release from the old version to update automatically and so you (the user) have to do this manually on your machine (sorry).

The old version might continue working for a few use cases for some time,
but it is unsupported and many things will stop working after 6.2.
It is recommended that everybody update to the new OverlayPlugin fork as soon as possible.
Hopefully this will be the last time this has to happen, fingers crossed.
(Sorry for the hassle!)

## 1) Remove the old OverlayPlugin

Open ACT.
Go to `Plugins` -> `Plugin Listing`.
For every box in the left column that says `OverlayPlugin.dll`, click the ❌ button on that box.
It will prompt you `Are you sure you wish to remove plugin OverlayPlugin.dll?`.  
Click `Yes`.

Repeat this process until there are no plugins in the list that say `OverlayPlugin.dll`.
You need to do this even if you got the old plugin from `Get Plugins`.

Once you are done, close ACT entirely.

## 2) Close ACT

Now that you have removed the old OverlayPlugin,
close ACT entirely.

## 3) Add the new OverlayPlugin

Reopen ACT.
If you have not closed ACT after step 1, please close it now and reopen it.

Go to `Plugins` -> `Plugin Listing`. 
In the upper right corner of ACT, click `Get Plugins...`.
Select `(86) Overlay Plugin`.
Click `Download and Enable`.
If successful, it will say `The plugin has been added and started.`
Close the `Get Plugins` window.

## 4) Reorder your plugins

Open ACT if needed.
Go to `Plugins` -> `Plugin Listing`. 
There will be an `OverlayPlugin.dll` listed at the bottom of the list in the left column.
If needed, click the ⬆️ button until `Overplugin.dll` is directly below `FFXIV_ACT_Plugin.dll`.
Said again, `FFXIV_ACT_Plugin.dll` should be the top entry in the list.
`OverlayPlugin.dll` should be the second entry in the list.

If you are using cactbot, then `CactbotOverlay.dll` should be below `OverlayPlugin.dll`.  In other words, `FFXIV_ACT_Plugin.dll` on top, then `OverlayPlugin.dll`, then `CactbotOverlay.dll`.

## 5) Close and reopen ACT

During this process, ACT may prompt you with `ACT Restart Requested` and `Restarting ACT is required to complete changes`.
You are welcome to click this any time it shows up and restart ACT.
However, once you are done with all of the above three steps,
click `Restart` or manually close and reopen ACT yourself one final time.

## Known Errors

If OverlayPlugin prompts you for an update with a blank changelog and errors
that says `The download was interrupted` and `"https://github.com/ngld/OverlayPlugin/releases/download/v0.19.1/OverlayPlugin-0.19.1.7z" failed with code: 404` then it is likely that you have partially updated.  Start again from step 1 above, make sure all OverlayPlugin.dll entries have been removed, and then make sure that you close ACT after this step.

## Need Help?

If you experience any issues or would like further assistance, please ask for help in the [ACT_FFXIV](https://discord.gg/hK523Pj) discord.
