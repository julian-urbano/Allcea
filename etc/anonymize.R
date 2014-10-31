setwd("M:/Programming/Allcea/etc/")
rm(list=ls(all=TRUE))
require(digest)

foo <- function(x) {
	return( substr(digest(x, algo="sha1",ascii=F), 0, 6) )
}

# Runs
t <- read.table("original.txt", head=T, sep="\t", stringsAsFactors=F)
u <- t
for(i in 1:length(u)){
	u[,i] <- sapply(t[,i], foo)
	if(length(unique(u[,i])) != length(unique(t[,i])))
		stop(i)
}
u <- u[c("system","query","doc")]
write.table(file="runs.txt", col.names=F, row.names=F, quote=F, sep="\t", u)

# Judgments
u$fine <- t$fine
u <- u[-1]
u <- unique(u)
u <- u[order(u$query, u$doc),]
write.table(file="judgments.txt", col.names=F, row.names=F, quote=F, sep="\t", u)

# Metadata
t <- read.table("original-meta.txt", head=T, sep="\t", stringsAsFactors=F, quote="\"")
t <- t[c("id","track_artist","genre")]
u <- t
for(i in 1:length(u)){
	u[,i] <- sapply(t[,i], foo)
	if(length(unique(u[,i])) != length(unique(t[,i])))
		stop(i)
}
u <- u[order(u$id),]
write.table(file="metadata.txt", col.names=F, row.names=F, quote=F, sep="\t", u)
