import jobs.generation.Utilities;

def project = GithubProject
def branch = GithubBranchName


[true, false].each { isPR ->
    ['Windows_NT'].each { os ->

        def newJob = job(Utilities.getFullJobName(project, os.toLowerCase(), isPR)) {}
    }
}
