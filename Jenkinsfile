node {
  stage('CheckOut'){
    checkout([$class: 'GitSCM', branches: [[name: '*/master']], doGenerateSubmoduleConfigurations: false, extensions: [[$class: 'SubmoduleOption', disableSubmodules: false, parentCredentials: false, recursiveSubmodules: true, reference: '', trackingSubmodules: false]], submoduleCfg: [], userRemoteConfigs: [[url: 'https://github.com/southernwind/ScrapingService']]])
  }

  stage('Configuration'){
    configFileProvider([configFile(fileId: 'f685dfc0-54a0-4999-b868-baf82fecb0e9', targetLocation: 'ScrapingService/appsettings.json')]) {}
  }

  stage('Build ScrapingService'){
    dotnetBuild configuration: 'Debug', project: 'ScrapingService.sln', sdk: '.NET8', unstableIfWarnings: true
  }

  withCredentials( \
      bindings: [sshUserPrivateKey( \
        credentialsId: 'ac005f9d-9b4b-496f-873c-1c610df01c03', \
        keyFileVariable: 'SSH_KEY', \
        usernameVariable: 'SSH_USER')]) {
    stage('Deploy ScrapingService'){
      sh 'scp -pr -i ${SSH_KEY} ./ScrapingService/bin/Debug/net8/* ${SSH_USER}@home-server.localnet:/opt/scraping-service'
    }

    stage('Restart ScrapingService'){
      sh 'ssh home-server.localnet -t -l ${SSH_USER} -i ${SSH_KEY} sudo service scraping restart'
    }
  }

  stage('Notify Slack'){
    sh 'curl -X POST --data-urlencode "payload={\\"channel\\": \\"#jenkins-deploy\\", \\"username\\": \\"jenkins\\", \\"text\\": \\"スクレイピングサービスのデプロイが完了しました。\\nBuild:${BUILD_URL}\\"}" ${WEBHOOK_URL}'
  }
}