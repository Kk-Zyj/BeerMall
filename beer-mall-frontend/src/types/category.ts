export interface Category {
  id: number; 
  name: string;
  icon: string; // 注意：后端如果是 PascalCase，JSON 序列化默认会转为 camelCase
  sort: number;  
}