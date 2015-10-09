# PeVerifyFolder
Run PeVerify.exe on all files in a folder recursively 

[PeVerify documentation page](https://msdn.microsoft.com/en-us/library/62bwd2yd(v=vs.110).aspx)

## Usage
* In Program.cs change `binDirectoryPath` to the folder you want to check.
* In PeVerify.cs change `_peVerifyPath`.  Make _peVerifyPath null if you want it to attempt autodiscover the path.