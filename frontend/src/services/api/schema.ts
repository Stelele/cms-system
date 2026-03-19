export interface BlogResponse {
  id: string;
  name: string;
  slug: string;
  description: string;
  createdOn: string;
  updatedOn: string;
}

export interface PostResponse {
  id: string;
  blogId: string;
  title: string;
  slug: string;
  content: string;
  tag: string;
  coverImageUrl: string | null;
  publishedOn: string | null;
  isPublished: boolean;
  createdOn: string;
  updatedOn: string;
}

export interface TagResponse {
  tag: string;
  count: number;
}

export interface CreateBlogCommand {
  name: string;
  slug: string;
  description: string;
}

export interface CreatePostCommand {
  title: string;
  slug: string;
  content: string;
  tag: string;
  coverImageUrl?: string;
  isPublished: boolean;
}
