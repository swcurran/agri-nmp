node('maven') {
    
	// Code Quality Check is not possible for a C# project, as it must be run from a MS Windows server or PC.
	// See the project readme for instructions.
	
	// Since the code quality check is not run here, then there is also no need to checkout the source.
	    
	stage('build') {
	 echo "Building..."
	 openshiftBuild bldCfg: 'nmp', showBuildLogs: 'true'
	 openshiftTag destStream: 'nmp', verbose: 'true', destTag: '$BUILD_ID', srcStream: 'nmp', srcTag: 'latest'
	 openshiftTag destStream: 'nmp', verbose: 'true', destTag: 'dev', srcStream: 'nmp', srcTag: 'latest'
    }
	
	stage('validation') {
          dir('functional-tests'){
                 sh './gradlew --debug --stacktrace phantomJsTest'
      }
   }
}


stage('deploy-test') {
  input "Deploy to test?"
  
  node('master'){
     openshiftTag destStream: 'nmp', verbose: 'true', destTag: 'test', srcStream: 'nmp', srcTag: '$BUILD_ID'
  }
}

stage('deploy-prod') {
  input "Deploy to prod?"
  node('master'){
     openshiftTag destStream: 'nmp', verbose: 'true', destTag: 'prod', srcStream: 'nmp', srcTag: '$BUILD_ID'
  }
  
}

