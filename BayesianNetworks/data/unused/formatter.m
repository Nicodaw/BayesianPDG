%load the dungeon and the room dataset as you see fit
mixed = []

% for i = 1:length(lidungeondata)
%     for j = 1:lidungeondata(i)
%         mixed(i,:) = [roomdata(i,:) lidungeondata(j,:)];
%         i = i + 1;
%     end
% end

% for i = 1:length(dungeondata)
%     for j = 1:dungeondata(i)
%         mixed = [mixed; dungeondata(i,:)];
%     end
% end

saveCSV(comb, "test")


function saveCSV(m, filename)
  r = mat2cell(m, [length(m)/3 length(m)/3 length(m)/3]);
  celldisp(r(1));
   for i = length(r)
       writecell(r(i), strcat(num2str(i), 'part_combdata.csv'));
   end
  
end