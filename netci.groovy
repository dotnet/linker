import jobs.generation.Utilities;

def project = GithubProject
def branch = GithubBranchName

def static setRecursiveSubmoduleOption(def job) {
    job.with {
        configure {
            it / 'scm' / 'extensions' << 'hudson.plugins.git.extensions.impl.SubmoduleOption' {
                recursiveSubmodules(true)
            }
        }
    }
}

[true, false].each { isPR ->
    ['Windows_NT'].each { os ->

        def jobName = Utilities.getFullJobName(project, os.toLowerCase(), isPR)
        out.println("job name: ${jobName}")

        def newJob = job(jobName) {}

        if (os == 'Windows_NT') {
            newJob.with {
                steps {
                    batchFile("build.cmd")
                }
            }
        } else if (os == 'Ubuntu') {
            newJob.with {
                steps {
                    shell("build.sh")
                }
            }
        }
        
        Utilities.setMachineAffinity(newJob, os, 'latest-or-auto')

        Utilities.standardJobSetup(newJob, project, isPR, "*/${branch}")
        setRecursiveSubmoduleOption(newJob)

        if (isPR) {
            Utilities.addGithubPRTriggerForBranch(newJob, branch, "${os} Build")
        } else {
            Utilities.addGithubPushTrigger(newJob)
        }
    }
}

out.println("done generating jobs!")
