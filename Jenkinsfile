pipeline {
  agent { node { label 'docker' } }
  environment {
      VERSION_LIB = getVersionFromCsProj('Terradue.Portal/Terradue.Portal.csproj')
      VERSION_TYPE = getTypeOfVersion(env.BRANCH_NAME)
      CONFIGURATION = getConfiguration(env.BRANCH_NAME)
      JENKINS_API_TOKEN = credentials('jenkins_api_token_repository')      
  }
  stages {
    stage('.Net Core') {
      agent {           
          dockerfile {
            additionalBuildArgs "-t dotnet/sdk-mono-tep:6.0 --build-arg JENKINS_API_TOKEN=${env.JENKINS_API_TOKEN}"
          }
      }
      environment {
        DOTNET_CLI_HOME = "/tmp/DOTNET_CLI_HOME"
      }
      stages {
        stage("Build & Test") {
          steps {
            echo "Build .NET application"
            sh "dotnet nuget add source https://repository.terradue.com/artifactory/api/nuget/nuget-release --name t2 --username jenkins --password ${env.JENKINS_API_TOKEN} --store-password-in-clear-text"            
            // sh "dotnet restore ./"
            // sh "dotnet restore Terradue.Cloud/Terradue.Cloud.csproj"
            // sh "dotnet restore Terradue.News/Terradue.News.csproj"
            // sh "dotnet restore Terradue.Authentication/Terradue.Authentication.csproj"
            // sh "dotnet build -c ${env.CONFIGURATION} --no-restore ./"
            // sh "dotnet test -c ${env.CONFIGURATION} --no-build --no-restore ./"
            // sh "dotnet restore Terradue.Portal/Terradue.Portal.csproj"
            sh "dotnet build -c ${env.CONFIGURATION} Terradue.Portal.AdminTool"
            sh "dotnet build -c ${env.CONFIGURATION} Terradue.Portal.Agent"
            sh "dotnet build -c ${env.CONFIGURATION} Terradue.Portal"
            sh "dotnet test -c ${env.CONFIGURATION} --no-build --no-restore ./"
          }
        }
        stage('Publish NuGet') {
          when{
            branch pattern: "(release\\/[\\d.]+|master)", comparator: "REGEXP"
          }
          steps {
            withCredentials([string(credentialsId: 'nuget_token', variable: 'NUGET_TOKEN')]) {
              sh "dotnet pack Terradue.Portal/Terradue.Portal.csproj -c ${env.CONFIGURATION} -o publish"
              // sh "dotnet pack Terradue.Cloud/Terradue.Cloud.csproj -c ${env.CONFIGURATION} -o publish"
              // sh "dotnet pack Terradue.News/Terradue.News.csproj -c ${env.CONFIGURATION} -o publish"
              // sh "dotnet pack Terradue.Authentication/Terradue.Authentication.csproj -c ${env.CONFIGURATION} -o publish"
              sh "dotnet nuget push publish/*.nupkg --skip-duplicate -k $NUGET_TOKEN -s https://api.nuget.org/v3/index.json"
            }
          }
        }
      }
    }
  }
}

def getTypeOfVersion(branchName) {
  def matcher = (branchName =~ /(v[\d.]+|release\/[\d.]+|master)/)
  if (matcher.matches())
    return ""
  
  return "dev"
}

def getConfiguration(branchName) {
  def matcher = (branchName =~ /(release\/[\d.]+|master)/)
  if (matcher.matches())
    return "Release"
  
  return "Debug"
}

def getVersionFromCsProj (csProjFilePath){
  def file = readFile(csProjFilePath) 
  def xml = new XmlSlurper().parseText(file)
  def suffix = ""
  if ( xml.PropertyGroup.VersionSuffix[0].text() != "" )
    suffix = "-" + xml.PropertyGroup.VersionSuffix[0].text()
  return xml.PropertyGroup.Version[0].text() + suffix
}
