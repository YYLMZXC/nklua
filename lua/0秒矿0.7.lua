jr = jr()
jr:init(9527)

--初始秒矿进度修改
fcode = "83 EC 08 0F B7 07 8B CC"
ret = jr:searchFcode("netcraft.exe",fcode)
print(ret)
ret=ret-4
jr:writeBytes(ret,"33 33 33 3F")

-- 秒矿间隔删除
fcode = "8B 8F E4 00 00 00 F6 41 38 01 74"
ret = jr:searchFcode("netcraft.exe",fcode)
print(ret)
ret=ret-2
jr:writeBytes(ret,"90 90")

-- 秒矿进度增加禁止修改
fcode = "9F F6 C4 44 7A 14 B0 01 5E 8B 4C 24 18"
ret = jr:searchFcode("netcraft.exe",fcode)
print(ret)
ret=ret-4
jr:writeBytes(ret,"90 90 90 90")
