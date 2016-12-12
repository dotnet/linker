import jobs.generation.Utilities;

def project = GithubProject
def branch = GithubBranchName


[true, false].each { isPR ->
    ['Windows_NT'].each { os ->

        def jobName = Utilities.getFullJobName(project, os.toLowerCase(), isPR)
        out.println("job name: ${jobName}")
        
        def newJob = job(jobName) {
            steps {
                batchFile('build.cmd')
            }
        }
    }
}
