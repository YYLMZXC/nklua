jr = jr()
jr:init(9527)
fcode = "74 21 8B 4F 1C 8B 41 10"
ret = jr:searchFcode("netcraft.exe",fcode)
print(ret)
ret=ret-7
jr:writeBytes(ret,"D9 5E 08")

--原有代码
--jr:writeBytes(ret,"D9 5E 08")