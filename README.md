![collectiontrans](https://cloud.githubusercontent.com/assets/7772659/7116407/baec709c-e1f1-11e4-994c-85c7e2b61764.png)
# ClipTex mini IDE
A comfortable mini IDE for Latex. The rendered image is always saved to the clipboard as a .png. Handy for using latex in chats.

The program is written in C# for use under Windows.
When you use it you want to open it by a keyboard shortcut, immediately write a few lines of latex code (by default in math mode), hit Ctrl-Enter and insert the picture that is already in the clipboard wherever you need it.</p>

It's all about speed and comfortability, providing use by shortcuts, autocomplete, placeholders in formulas between which you can jump and other clever IDE features.

My scenario is inserting formulas in the telegram desktop client.

<p>
<p>

####Get it to work:
A previously installed latex environment is needed.<p>
latex.exe is needed in PATH<br>
dvips.exe is needed in PATH

You may have an installed convert.exe. And it may be the wrong one. If ClipTex returns an error regarding convert.exe
try to remove convert.exe and install ImageMagick (don't uncheck 'add to Path').<br>
http://www.imagemagick.org/script/binary-releases.php#windows

Maybe ghostscript is also needed<br>
http://www.ghostscript.com/download/gsdnld.html
