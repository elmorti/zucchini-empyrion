Remove-Item -Path .\Output\EmpyrionModdingFramework -Recurse -Force -ErrorAction Ignore
New-Item -Path .\Output\EmpyrionModdingFramework -ItemType Container -Force
Copy-Item -Path .\EmpyrionModdingFramework\bin\Debug\ILMerge\EmpyrionModdingFramework.dll .\Output\EmpyrionModdingFramework
Copy-Item -Path .\Config\Config.yaml .\Output\EmpyrionModdingFramework

