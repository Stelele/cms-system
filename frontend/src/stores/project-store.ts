import { BackendApiSingleton } from '@/services/backend'
import type { components } from '@/services/backend/schema'
import { defineStore } from 'pinia'
import { ref } from 'vue'

type BlogKind = components['schemas']['BlogKind']
type ProjectCategory = components['schemas']['ProjectCategory']
type ProjectSummary = components['schemas']['ProjectSummaryResponse']
type ProjectResponse = components['schemas']['ProjectResponse']
type ProjectLinkInput = components['schemas']['ProjectLinkInput']
type ProjectStatus = components['schemas']['ProjectStatus']

/**
 * The three project blogs, mirroring the backend's ProjectBlogs so the admin
 * cannot offer a category the API will refuse. The backend is the authority and
 * re-checks the mapping on every write; this copy exists so the UI can label a
 * blog as a project category before a project exists in it.
 */
export const PROJECT_BLOGS: ReadonlyArray<{
  slug: string
  category: ProjectCategory
  label: string
}> = [
  { slug: 'game-dev', category: 'GameDev', label: 'Game Dev' },
  { slug: 'graphics', category: 'Graphics', label: 'Graphics' },
  { slug: 'business-case', category: 'BusinessCase', label: 'Business Case' },
]

export function categoryForBlogSlug(slug: string | undefined): ProjectCategory | null {
  if (!slug) return null
  return PROJECT_BLOGS.find((b) => b.slug === slug)?.category ?? null
}

export type ProjectFormData = {
  category: ProjectCategory
  year: number | string
  stack: string[]
  lastPushedAt: string | null
  links: ProjectLinkInput[]
  status: ProjectStatus
}

export const useProjectStore = defineStore('projectStore', () => {
  const projects = ref<ProjectResponse[]>([])
  const isLoading = ref(false)

  function toApiLinks(links: ProjectLinkInput[]): ProjectLinkInput[] {
    return links
      .map((l) => ({ label: l.label?.trim() ?? '', url: l.url?.trim() ?? '' }))
      .filter((l) => l.label.length > 0 && l.url.length > 0)
  }

  /**
   * Reads through the project endpoints rather than the post endpoints, so the
   * typed fields arrive. The post list for a project blog would return rows
   * without category, year, stack or links.
   */
  async function fetchProjects(): Promise<ProjectSummary[]> {
    isLoading.value = true
    try {
      const client = await BackendApiSingleton.getInstance()
      const { data, error } = await client.GET('/projects')
      if (error) throw new Error('Failed to load projects')
      return data ?? []
    } finally {
      isLoading.value = false
    }
  }

  async function fetchProjectBySlug(slug: string): Promise<ProjectResponse | null> {
    const client = await BackendApiSingleton.getInstance()
    const { data, error } = await client.GET('/projects/{slug}', {
      params: { path: { slug } },
    })
    if (error) return null
    return data ?? null
  }

  async function createProject(
    blogId: string,
    data: {
      title: string
      slug: string
      content: string
      description: string | null
      coverImageUrl: string | null
      isPublished: boolean
    } & ProjectFormData,
  ): Promise<string | null> {
    const client = await BackendApiSingleton.getInstance()
    const { data: response, error } = await client.POST('/projects', {
      body: {
        blogId,
        title: data.title,
        slug: data.slug,
        content: data.content,
        description: data.description,
        category: data.category,
        year: data.year,
        stack: data.stack,
        lastPushedAt: data.lastPushedAt,
        links: toApiLinks(data.links),
        coverImageUrl: data.coverImageUrl,
        isPublished: data.isPublished,
      },
    })
    if (error) return null
    return response ?? null
  }

  async function updateProject(
    blogId: string,
    id: string,
    data: {
      title: string
      slug: string
      content: string
      description: string | null
      coverImageUrl: string | null
      isPublished: boolean
    } & ProjectFormData,
  ): Promise<boolean> {
    const client = await BackendApiSingleton.getInstance()
    const { error } = await client.PUT('/projects/{id}', {
      params: { path: { id } },
      body: {
        blogId,
        id,
        title: data.title,
        slug: data.slug,
        content: data.content,
        description: data.description,
        category: data.category,
        year: data.year,
        stack: data.stack,
        lastPushedAt: data.lastPushedAt,
        links: toApiLinks(data.links),
        coverImageUrl: data.coverImageUrl,
        isPublished: data.isPublished,
      },
    })
    return !error
  }

  function isProjectBlog(blog: { slug: string; kind: BlogKind } | undefined | null): boolean {
    return !!blog && blog.kind === 'Project'
  }

  return {
    projects,
    isLoading,
    fetchProjects,
    fetchProjectBySlug,
    createProject,
    updateProject,
    isProjectBlog,
  }
})
