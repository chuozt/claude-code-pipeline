#!/bin/bash
perl -0777 -ne '
  BEGIN{ local $/="
"; open(F,"ss.txt"); @ss=<F>; chomp @ss; }
  while (/<row r="(\d+)"[^>]*(?:\/>|>(.*?)<\/row>)/gs) {
    my ($r,$row)=($1,$2); next unless defined $row; my @cells;
    while ($row=~/<c ([^>]*?)(?:\/>|>(.*?)<\/c>)/gs) {
      my ($attr,$body)=($1,$2);
      next unless defined $body;
      my ($col)= $attr=~/r="([A-Z]+)\d+"/;
      my ($t)  = $attr=~/t="(\w+)"/;
      my $val;
      if (defined $t && $t eq "s") {
        my ($v)= $body=~/<v>(\d+)<\/v>/; next unless defined $v; $val=$ss[$v];
      } elsif ($body=~/<is>/ or (defined $t and $t eq "str")) {
        my $s=""; $s.=$1 while $body=~/<t[^>]*>(.*?)<\/t>/gs;
        $s=($body=~/<v>(.*?)<\/v>/s)[0] // $s if $s eq "";
        $val=$s;
      } else {
        ($val)= $body=~/<v>(.*?)<\/v>/s;
      }
      next unless defined $val && $val ne "";
      $val=~s/&amp;/&/g; $val=~s/&lt;/</g; $val=~s/&gt;/>/g; $val=~s/&quot;/"/g;
      $val=~s/&#10;/ ⏎ /g; $val=~s/[\r\n]+/ ⏎ /g; $val=~s/\s+/ /g;
      push @cells, "$col: $val";
    }
    print "R$r| ".join(" | ",@cells)."\n" if @cells;
  }' "$1"
