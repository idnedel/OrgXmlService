# 📂 OrgXmlService  

> :construction: Projeto em construção... :construction:

Serviço Windows para **organização automática de arquivos XML fiscais** (NFe, CTE, MDFe, NFSe, Eventos) em estrutura de pastas baseada em **CNPJ, ano e mês**.  

---

## Funcionalidades  

- 📡 Monitoramento contínuo de pasta de origem para novos arquivos XML; 
- 📑 Classificação automática por tipo de documento fiscal; 
- 📂 Organização por estrutura; 

- ✅ Suporte a múltiplos tipos:  
	- NFe (Nota Fiscal Eletrônica)  
	- CTE (Conhecimento de Transporte Eletrônico)  
	- MDFe (Manifesto de Documentos Fiscais)  
	- NFSe (Nota Fiscal de Serviços Eletrônica)  
	- Eventos (Eventos fiscais)  
- ⚠️ Tratamento de erros com pasta dedicada para arquivos problemáticos;
- 📝 Logs detalhados com **Serilog**;
- 🔒 Lista de CNPJs permitidos para organização diferenciada (apenas NFe).

---

## Pré-requisitos  

- [.NET 6.0](https://dotnet.microsoft.com/en-us/download/dotnet/6.0) ou superior  
- Windows Server/Desktop  
- Permissões de **leitura/escrita** nas pastas configuradas  

---

## Configuração como Serviço Windows  

### Publicação do Projeto  

`dotnet publish -c Release -r win-x64 --self-contained`

---

## Configuração das Pastas

No arquivo **`Worker.cs`**, configure os diretórios conforme necessidade:  

`private readonly string origem = @"C:\XML\pasta_origem_xml";`

`private readonly string destinoBase = @"C:\XML\pasta_destino_xml";`

`private readonly string erro = @"C:\XML\pasta_erros_xml";`

Crie o arquivo cnpjs.txt na raiz do projeto com os CNPJs permitidos, um por linha.
- 12345678000195
- 98765432000186

---

## Instalação do Serviço

### Navegue até a pasta de publicação pelo terminal do windows
`cd bin\Release\net6.0\win-x64\publish`

### Instale o serviço
`sc create OrgXmlService binPath= "C:\caminho\completo\OrgXmlService.exe" obj= "DOMINIO\usuario" password= "SENHA"`

## Gerenciamento do Serviço pelo terminal do windows

### Iniciar serviço
`sc start OrgXmlService`

### Parar serviço
`sc stop OrgXmlService`

### Remover serviço
`sc delete OrgXmlService`

### Ver status
`sc query OrgXmlService`

