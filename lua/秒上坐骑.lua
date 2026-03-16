jr = jr()
jr:init(9527)
fcode = "2B 4F 1C 03 C8 80 7F 25 00"
ret = jr:searchFcode("netcraft.exe",fcode)
print(ret)
ret=ret-7
jr:writeBytes(ret,"69 CE D0 07 00 00")

--原有代码
--jr:writeBytes(ret,"69 CE E8 03 00 00")