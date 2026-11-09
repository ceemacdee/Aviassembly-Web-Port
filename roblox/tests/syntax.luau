-- Compiles every file passed in ARGS (syntax check only).
local bad = 0
for i = 2, #ARGS do
	local f = ARGS[i]
	local ok, err = pcall(__loadstring, __readfile(f), f)
	if not ok then
		bad += 1
		print("SYNTAX ERROR", err)
	end
end
print(("checked %d files, %d with errors"):format(#ARGS - 1, bad))
