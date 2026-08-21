/** Every route in one place, so links and navigation cannot drift apart. */
const Paths = {
  Home: '/',
  DiscussionPattern: '/discussions/:id',
  discussion: (id: string) => `/discussions/${id}`,
} as const;

export default Paths;
