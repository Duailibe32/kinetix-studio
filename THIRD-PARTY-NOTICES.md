# Dependências de terceiros

`ViGEmClient.dll` x64 é extraída do recurso `costura64.vigemclient.dll` do pacote **Nefarius.ViGEm.Client 1.21.256**, obtido no NuGet oficial. A aplicação chama a API C pública diretamente. O driver ViGEmBus é uma instalação externa e não faz parte deste pacote.

- Pacote: https://www.nuget.org/packages/Nefarius.ViGEm.Client/1.21.256
- Código: https://github.com/nefarius/ViGEmClient e https://github.com/nefarius/ViGEm.NET
- Copyright (c) 2017-2023 Nefarius Software Solutions e.U. / Benjamin Höglinger-Stelzer
- Licença MIT, conforme os metadados do pacote. Os textos oficiais estão em `dependencies/LICENSE-ViGEmClient` e `dependencies/LICENSE-ViGEm.NET` no projeto, consolidados integralmente em `LICENSES.txt` na distribuição.

O instalador é produzido com Inno Setup 6.7.3, de Jordan Russell e Martijn Laan: https://jrsoftware.org/isinfo.php. O código-fonte e os termos estão em https://github.com/jrsoftware/issrc. O compilador é ferramenta de desenvolvimento e não integra o payload do aplicativo.

O Kinetix Studio não é afiliado a Nefarius, Sony, Microsoft ou reWASD. PlayStation, DualShock e Xbox identificam os dispositivos emulados.
