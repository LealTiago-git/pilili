# Pilili

Toca o som de fim da urna eletrônica toda vez que você aperta **Enter**, em qualquer programa do Windows.

Roda em segundo plano, com um ícone na bandeja do sistema (perto do relógio).

## Uso

1. Baixe o `Pilili.exe` em [Releases](../../releases) e dê dois cliques nele.
2. Aperte Enter em qualquer lugar.

Clique no ícone da bandeja para abrir o painel:

- **Volume**: arraste a barra ou use as setas do teclado. Ao soltar, toca uma prévia.
- **Testar som**: toca o som.
- **Sair**: fecha o programa.

O volume fica salvo em `%APPDATA%\PililiVolume.txt` e é mantido mesmo se o PC desligar sem fechar o programa.

Segurar Enter toca o som uma vez só. Abrir o programa de novo não cria uma segunda cópia.

## Requisitos

- Windows 11 (no Windows 10 funciona, mas o painel perde os cantos arredondados)
- .NET Framework 4.x, que já vem com o Windows

## Compilar

Não precisa instalar nada: o compilador C# já vem com o Windows. Na pasta do projeto:

```
C:\Windows\Microsoft.NET\Framework64\v4.0.30319\csc.exe /nologo /target:winexe /out:Pilili.exe /win32icon:pilili.ico /resource:fim-som-da-urna.mp3,som.mp3 /resource:pilili.ico,pilili.ico /r:System.Windows.Forms.dll /r:System.Drawing.dll Pilili.cs
```

Gera um `Pilili.exe` único, com o som e o ícone embutidos.

## Arquivos

| Arquivo | O que é |
|---|---|
| `Pilili.cs` | Todo o código-fonte |
| `fim-som-da-urna.mp3` | O som |
| `pilili.ico` | O ícone |

## Antivírus

O programa escuta o teclado do sistema todo para detectar o Enter. Alguns antivírus podem desconfiar disso. O código só verifica a tecla Enter e não grava nem envia nada do que é digitado.

## Créditos

Ícone baseado em [check-to-slot](https://fontawesome.com/icons/check-to-slot) do [Font Awesome Free](https://fontawesome.com), licença [CC BY 4.0](https://creativecommons.org/licenses/by/4.0/).
