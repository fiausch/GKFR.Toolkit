all:
	dotnet build
	cp ./dist/* -t ~/.local/share/Steam/steamapps/common/Garfield\ Kart\ -\ Furious\ Racing/BepInEx/plugins/

