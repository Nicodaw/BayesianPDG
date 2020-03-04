[dNames,~,dId] = unique(lidungeondata.Name);
[rNames,~,rId] = unique(roomdata.Name);
%hardcoded unknown values to 100, remove afterwards!!! Also append the
%headers again...
dungeons = lidungeondata{:,[2,3]};
rooms = roomdata{:,[2,3,4]};

combined = [];

for i = 1:length(rId)
    combined = [combined; [rooms(i,:) dungeons(find(rId(i) == dId),:)]];
end

n = floor(length(combined)/3);

r1 = [1:1:n];
r2 = [n+1:1:n*2];
r3 = [(n*2)+1:1:length(combined)];

p1 = combined(r1,:);
p2 = combined(r2,:);
p3 = combined(r3,:);

writematrix(p1, "DR_1.csv");
writematrix(p2, "DR_2.csv");
writematrix(p3, "DR_3.csv");
writematrix(combined,"DR_data.csv");