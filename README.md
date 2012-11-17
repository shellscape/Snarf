Snarf
=====

Snarf is an NFS v2 server implementation written in C# with .NET 4.5.

Snarf is based in part on the JNFS project by Steven Procter @ http://void.org/~steven/jnfs/.

**Current Status**

11/16/2012
	* First commit/push. The server accepts version 2 connections and mounts. 
	* Mounts only list files and not directories. 
	* Mounts cannot persist between server restarts.

**But, Why?**

I started writing this for my XBMC-AppleTV2 setup. After upgrading to Windows 8 on the machine my movies are connected to, SMB started to get flaky. Very flaky. I gave HaneWin NFS a shot (and a few others, FreeNFS and winnfsd) and wasn't satisfied with them. 

**Down the Road**
	* Implement the v3 spec over TCP.

**Licensing**

Unless otherwise stated, the code here is licensed under the MIT license. Have at it!