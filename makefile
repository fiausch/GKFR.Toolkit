all:
	dotnet build
	cp ./dist/* -t /home/tera/.local/share/Steam/steamapps/common/Garfield\ Kart\ -\ Furious\ Racing/BepInEx/plugins/

