## server

O *server* é a aplicação server-side do Stocks. Possui as principais APIs, bancos de dados e outros componentes relacionados à infraestrutura do projeto que são obrigatórios para o funcionamento
do *stocks-client*.  

A aplicação é escrita em C#, utiliza o framework [.NET Core 6](https://dotnet.microsoft.com/en-us/download/dotnet/6.0) e o banco de dados relacional e código-aberto Postgres.    

O código-fonte pode ser desenvolvido, executado e publicado em todas as multiplataformas tais como Windows, macOS e distribuições Linux.  

## Documentação para desenvolvedores

A execução da aplicação pode ser feita através do Docker. Para isso, certifique-se que você possui o [Docker instalado](https://www.docker.com/products/docker-desktop/) em sua máquina.
   
1. Faça o download da aplicação na sua máquina local:
   
   ```
   git clone https://github.com/calculadora-ir-stocks/server.git  
   ```
  
2. Acesse o diretório da aplicação:

   ```
   $ cd server/
   ```
   
3. Execute a aplicação através do Docker Compose na raíz do projeto:

   ```
   $ docker compose -f compose-dev.yaml up -d
   ```

Caso os contâiners sejam pausados, execute o mesmo comando novamente para iniciá-los.
O Swagger UI é utilizado para a documentação de todos os endpoints do servidor. Para visualizá-lo, acesse `http://localhost:8080/swagger/index.html`.
