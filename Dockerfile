# 프론트엔드 빌드 스테이지
FROM node:20 AS frontend-build
WORKDIR /frontend
COPY Frontend/package.json Frontend/package-lock.json ./
RUN npm install
COPY Frontend/ ./
RUN npm run build

# 백엔드 빌드 스테이지
FROM mcr.microsoft.com/dotnet/sdk:8.0 AS build
WORKDIR /src
COPY ["AutoInvest.csproj", "./"]
RUN dotnet restore "AutoInvest.csproj"
COPY . .
RUN dotnet publish "AutoInvest.csproj" -c Release -o /app/publish /p:UseAppHost=false

# 런타임 스테이지
FROM mcr.microsoft.com/dotnet/aspnet:8.0 AS final
WORKDIR /app
COPY --from=build /app/publish .
COPY --from=frontend-build /frontend/dist ./wwwroot

# 한국 시간대(KST) 설정 패키지 설치 (무인 설치 옵션 추가)
ENV DEBIAN_FRONTEND=noninteractive
RUN apt-get update && apt-get install -y tzdata && \
    ln -snf /usr/share/zoneinfo/Asia/Seoul /etc/localtime && echo Asia/Seoul > /etc/timezone

# Render.com 포트 바인딩 대응
ENV ASPNETCORE_URLS=http://+:5000
EXPOSE 5000

ENTRYPOINT ["dotnet", "AutoInvest.dll"]
