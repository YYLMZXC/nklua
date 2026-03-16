
jr = jr()
jr:init(9527)

--初始秒矿进度复原
fcode = "83 EC 08 0F B7 07 8B CC"
ret = jr:searchFcode("netcraft.exe",fcode)
print(ret)
ret=ret-4
jr:writeBytes(ret,"00 00 00 00")

--秒矿间隔复原
fcode = "8B 8F E4 00 00 00 F6 41 38 01 74"
ret = jr:searchFcode("netcraft.exe",fcode)
print(ret)
ret=ret-2
jr:writeBytes(ret,"72 54")

--秒矿进度增加禁止复原
fcode = "9F F6 C4 44 7A 14 B0 01 5E 8B 4C 24 18"
ret = jr:searchFcode("netcraft.exe",fcode)
print(ret)
ret=ret-4
jr:writeBytes(ret,"F3 0F 11 00")