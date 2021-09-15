#r "nuget: SharpZipLib, 1.2.0"
#r "nuget: SimpleExec, 6.2.0"

using ICSharpCode.SharpZipLib.Tar;
using ICSharpCode.SharpZipLib.GZip;
using static SimpleExec.Command;

using (var fs = File.Open("NextGenAudio.tar", FileMode.Create, FileAccess.Write))
using (var tar = TarArchive.CreateOutputTarArchive(fs))
using (var read = File.OpenRead("NextGenAudio"))
{
	var entry = TarEntry.CreateEntryFromFile("NextGenAudio");
	entry.TarHeader.Mode = Convert.ToInt32("0755", 8);
	tar.WriteEntry(entry, false);
}

Run("tar", "-rf NextGenAudio.tar WebInterface");

using (var fs = File.Open("NextGenAudio.tar.gz", FileMode.Create, FileAccess.Write))
using (var gz = new GZipOutputStream(fs))
using (var read = File.OpenRead("NextGenAudio.tar"))
{
	read.CopyTo(gz);
}
