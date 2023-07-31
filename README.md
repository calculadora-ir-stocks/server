## stocks-server

O *stocks-server* é a aplicação server-side do Stocks. Possui as principais APIs, bancos de dados e outros componentes relacionados à infraestrutura do projeto que são obrigatórios para o funcionamento
do *stocks-client*.  

A aplicação é escrita em C#, utiliza o framework [.NET Core 6](https://dotnet.microsoft.com/en-us/download/dotnet/6.0) e o banco de dados relacional e código-aberto utilizado é o Postgres.    

O código-fonte pode ser desenvolvido, rodado e publicado em todas as multiplataformas tais como Windows, macOS e distribuições Linux.  

## Documentação para desenvolvedores

1. Certifique-se que você possui o .NET Core 6 instalado em sua máquina. Para isso, [realize o download](https://dotnet.microsoft.com/en-us/download/dotnet/6.0) na página oficial da Microsoft.
   
2. Faça o download da aplicação na sua máquina local:
   
   ```
   git clone https://github.com/calculadora-ir-stocks/stocks-server.git  
   ```
  
3. Acesse o diretório da aplicação:

   ```
   $ cd stocks-server/
   ```
   
4. Restaure todas as dependências do projeto:

   ```
   $ dotnet restore
   ```
  
5. Acesse o diretório Web da aplicação:

   ```
   $ cd stocks/
   ```

6. Por fim, rode a aplicação (dentro do diretório Web):
   ```
   $ dotnet run
   ```
